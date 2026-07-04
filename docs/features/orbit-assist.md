# Orbit assist

What this is: the "bus stop" mechanic — a one-button burn that parks you in a circular orbit
around whatever planet you're near, priced honestly in mass pulses.

Where: it appears automatically in the map's HUD readout panel whenever a planet is close enough
to matter; press its **Enter orbit** button, or the `O` key, when the window is open.

## The panel

When you're within range of a body's Hill sphere (its true gravitational neighborhood), a strip
appears:

```
🛰 Orbit <body> — window OPEN / too fast (max 5.0 km/s rel) / get inside the Hill sphere
[Enter orbit (N p)]
```

Two progress bars underneath show, at a glance: your distance vs. the Hill sphere radius, and your
relative speed vs. the 5 km/s limit.

- **Window OPEN** — you're inside the Hill sphere, outside a minimum safety radius, and under
  5.0 km/s relative to the body. The button lights up green and is clickable.
- **too fast** — you're in range but moving too fast relative to the body to insert safely; brake
  (or trim with `Shift`+pulse) until you're under the limit.
- **get inside the Hill sphere** — you're not close enough yet; keep flying in.

## What "Enter orbit" does

It's an instant impulsive burn: your velocity is set to the body's velocity plus the local
circular orbital speed, preserving whichever way you were already swinging around it. The pulse
cost is proportional to the actual Δv needed — roughly one pulse per 1% of your current speed —
so a sloppy, fast approach costs more pulses than a gentle one. The button is disabled if you
don't have enough pulses to pay for it.

## Planned (armed) insertion

You don't have to wait next to a planet for the window to open — the [plotting desk](plotting-desk.md)
can **arm** an insertion at a planned closest pass, and the game fires it automatically the moment
your live flight enters the window, spending pulses for you. See that page for how to arm one.

## Tips

- The panel favors an armed target: if you've armed an insertion at a specific body, the panel
  tracks *that* body instead of whatever's merely nearest — so you don't get "Orbit Earth?" while
  you're sailing past it on the way to Mars.
- The sun itself never shows this panel — you already orbit it by definition.
- Once inserted, you're bound to that body (negative orbital energy, inside its Hill sphere) —
  the plotting desk's future nodes are marked stale, since your trajectory just changed out from
  under them.

See also: [depots.md](depots.md) for what's usually worth orbiting a planet *for*,
[dock-and-economy.md](dock-and-economy.md) for the three bodies where orbiting also means a
market.
