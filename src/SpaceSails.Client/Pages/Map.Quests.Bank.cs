using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Quests — the favor bank: the contact ledger it is kept in, the card at their table or over the wire, deposits, withdrawals, repayments, and a borrowed favor.
public partial class Map
{

    // The relationship system's seed (#185): the SAVED history of who we've done jobs for — a real
    // fact ("we now have a history with the lady at the Ringside bar") the future system reads.
    // PR-WIRE (FridaySecondPlan §0): the same book now also carries the favor bank's signed credit
    // balances (deposits, loans) via ContactLedger.ApplyCredit.
    private readonly ContactLedger _contacts = new();

    // PR-WIRE — the favor bank. One open bank card at a time (deposit / withdraw / borrow at a
    // contact), plus the favor debts we've taken on (each surfaces later as one quiet delivery in the
    // contact's voice — FavorObligation). Both are session state; the future save layer serializes the
    // ledger balances, not these transient UI holders.
    private BankSession? _bankSession;
    private readonly List<FavorObligation> _favorObligations = [];

    // The open bank card: whose desk we're at, their character sheet, and whether we reached them over
    // the wire (dark-web desk) or in person (their bar table) — the channel gates what's allowed.
    private sealed record BankSession(string ContactId, string DisplayName, ContactSheet Sheet, bool ViaWire)
    {
        public string? Notice { get; set; } // the last action's receipt, shown on the card
    }

    // Standard bank denominations for the card's quick buttons (the purse and balances are in credits).
    private static readonly long[] BankAmounts = [100, 500, 1000];

    // Open the favor-bank card for a contact. Channel-checked by the caller: in person (their bar) works
    // for anyone; over the wire only for a dark-web-native contact (ruling 6).
    private void OpenBank(string contactId, bool viaWire)
    {
        ContactSheet sheet = ContactSheets.For(contactId);
        if (viaWire && !FavorBank.CanBankRemotely(sheet))
        {
            ShowPulseMessage($"{sheet.DisplayName} won't touch the wire — that account is in person only. 🤝");
            return;
        }
        _bankSession = new BankSession(contactId, sheet.DisplayName, sheet, viaWire);
    }

    private void CloseBank() => _bankSession = null;

    // Open the bank at the bar patron you're standing at (the 'b' key, in person). Their character sheet
    // decides nothing here — every contact banks in person — but a stranger with no history still opens,
    // so a first deposit can seed a relationship you later borrow against.
    private void OpenBankAtBar()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.BarPatron } spot)
        {
            ShowPulseMessage("Stand at a contact's table to open an account. 💰");
            return;
        }
        string giver = spot.Label.Replace("◈", "").Trim();
        OpenBank(giver, viaWire: false);
    }

    // Park coin with a contact (the bank). Calm → the whole sum lands and earns interest; heated →
    // fencing: a dice-rolled cut proportional to heat is taken on the way in, ALWAYS less than the
    // collector would confiscate (FavorBank, ruling 5). Deposited coin is off the purse — invisible to
    // confiscation by construction (the BUSTED lane reads only _credits + carried cargo, never the
    // ledger balances). Returns quietly if the purse can't cover it.
    private void BankDeposit(long amount)
    {
        if (_bankSession is not { } session || amount <= 0)
        {
            return;
        }
        long park = Math.Min(amount, _credits);
        if (park <= 0)
        {
            session.Notice = "Nothing in the purse to park.";
            return;
        }

        int heat = _heat.Level;
        double roll = FavorBank.Roll($"{session.ContactId}|deposit|{SimTime:F0}|{park}");
        FavorBank.DepositQuote quote = FavorBank.PriceDeposit(park, heat, roll);

        _credits -= (int)park;                       // the whole sum leaves the purse
        _contacts.ApplyCredit(session.ContactId, session.DisplayName,
            FavorBank.DepositTxn(quote.Credited, SimTime,
                heat > 0 ? $"fenced {park:N0} cr (heat {heat})" : $"parked {park:N0} cr"));
        if (quote.Cut > 0)
        {
            _contacts.ApplyCredit(session.ContactId, session.DisplayName,
                FavorBank.FenceCutTxn(quote.Cut, SimTime, $"fence's cut, {quote.CutFraction * 100:F0}%"));
            session.Notice = $"Fenced {park:N0} cr while hot — {session.DisplayName} took {quote.Cut:N0} cr ({quote.CutFraction * 100:F0}%); {quote.Credited:N0} cr banked, safe from the collectors.";
        }
        else
        {
            session.Notice = $"Parked {park:N0} cr with {session.DisplayName} — off the purse, earning while it's quiet. 💰";
        }
        RequestVaultSave(); // #225: a bank move changed both the purse and the ledger balance
    }

    // Draw parked coin back out. In person or (for a wire contact) over the dark web — the channel was
    // enforced when the card opened. Only the positive part of the balance is ours to withdraw.
    private void BankWithdraw(long amount)
    {
        if (_bankSession is not { } session || amount <= 0)
        {
            return;
        }
        long balance = _contacts.For(session.ContactId).CreditBalance;
        long take = Math.Min(amount, balance);
        if (take <= 0)
        {
            session.Notice = "No coin of yours parked here to draw.";
            return;
        }

        _credits += (int)take;
        _contacts.ApplyCredit(session.ContactId, session.DisplayName,
            FavorBank.WithdrawalTxn(take, SimTime, $"drew {take:N0} cr"));
        session.Notice = $"Drew {take:N0} cr back into the purse from {session.DisplayName}.";
        RequestVaultSave(); // #225: purse + ledger balance changed
    }

    // Pay off an interest-bearing debt with coin on hand (drives a negative balance back toward zero).
    private void BankRepay(long amount)
    {
        if (_bankSession is not { } session || amount <= 0)
        {
            return;
        }
        long owed = -_contacts.For(session.ContactId).CreditBalance; // positive = what we owe
        long pay = Math.Min(Math.Min(amount, owed), _credits);
        if (pay <= 0)
        {
            session.Notice = owed <= 0 ? "You owe them nothing." : "Not enough coin to pay it down.";
            return;
        }

        _credits -= (int)pay;
        _contacts.ApplyCredit(session.ContactId, session.DisplayName,
            FavorBank.RepaymentTxn(pay, SimTime, $"repaid {pay:N0} cr"));
        session.Notice = $"Paid {pay:N0} cr toward what you owe {session.DisplayName}.";
        RequestVaultSave(); // #225: purse + ledger balance changed
    }

    // A modest, standard favor line — roughly a good top-up (≈ half a tank at the inner price). Kept
    // flat so the string (one quiet delivery) is always proportionate; never a fortune on a promise.
    private const long BankLoanPrincipal = 600;

    // Borrow the standard favor line from the contact whose bank card is open (in person or by wire).
    private void BorrowFavorFromBank()
    {
        if (_bankSession is not { } session)
        {
            return;
        }
        if (BankBorrowFavor(session.ContactId, BankLoanPrincipal, session.ViaWire))
        {
            session.Notice = $"{session.DisplayName} wires you {BankLoanPrincipal:N0} cr. You owe them one quiet delivery — it'll come. 📡";
        }
    }

    // Borrow gas money against a favor (the dream's anonymized wire). Books the principal as debt and
    // raises ONE quiet-delivery obligation that arrives later in the contact's voice; working that
    // delivery off IS the repayment. Trusted contacts only. Returns true if the wire went through.
    private bool BankBorrowFavor(string contactId, long principal, bool viaWire)
    {
        ContactSheet sheet = ContactSheets.For(contactId);
        int missions = _contacts.For(contactId).MissionsCompleted;
        if (!ContactSheets.WillStake(missions))
        {
            ShowPulseMessage($"{sheet.DisplayName} won't stake a captain they barely know — do a few jobs first. 🤝");
            return false;
        }
        if (viaWire && !sheet.CanWire)
        {
            ShowPulseMessage($"{sheet.DisplayName} deals in person only — no wire from here. 🤝");
            return false;
        }
        if (principal <= 0)
        {
            return false;
        }

        _credits += (int)principal;
        _contacts.ApplyCredit(contactId, sheet.DisplayName,
            FavorBank.BorrowTxn(principal, SimTime, $"favor wire — {principal:N0} cr gas money"));
        _favorObligations.Add(FavorObligation.ForLoan(sheet, principal, SimTime));
        RequestVaultSave(); // #225: a borrow books coin + a favor-debt obligation
        return true;
    }
}
