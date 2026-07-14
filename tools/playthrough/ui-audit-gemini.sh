#!/usr/bin/env bash
# Blind round: one screenshot, zero context, per the docs/ui-guidelines.md AI-playtest protocol.
cd "$(dirname "$0")"
export GEMINI_CLI_TRUST_WORKSPACE=true

ask () { # $1=png $2=task
  echo "=== $1 / task: $2"
  gemini -p "@$1 You are looking at a single screenshot of a browser game you have never seen. No manual, no other context. Answer concisely: (1) What is this screen for? (2) What actions do you believe you can take here — list at most 8. (3) Task: how exactly would you $2 from this screen — name the specific control you would use. If you cannot tell, say precisely what information is missing." > "${1%.png}.gemini.txt" 2>&1
  echo "--- wrote ${1%.png}.gemini.txt ($(wc -l < "${1%.png}.gemini.txt") lines)"
}

ask nav-map.png    "set course for Mars"
ask sensors.png    "find a ship you have lost track of"
ask warroom.png    "fire at a locked target"
ask trade.png      "sell your cargo"
ask comms.png      "buy route intel about a ship"
ask plot-sling.png "make your Jupiter flyby pass closer to the planet"
ask ledger.png     "start working the Roadster tip and find the wreck"
ask deck.png       "go ashore into the station"
echo ALL_DONE
