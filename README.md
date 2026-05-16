# Multi-Monitor Profiles — *Set it once. Recall it forever.*

Your monitors never sit still. One minute you're commanding a triple-monitor cockpit at your desk. The next, you've kicked back on the couch and you just want the TV running solo, full resolution, nothing else competing for attention.

Windows lets you *change* your display setup. It just doesn't let you **remember** it.

That's the gap this fills.

**Multi-Monitor Profiles** lets you capture the exact state of your displays—alignment, resolution, refresh rate, primary designation, the works—and bring it back instantly, on demand, by name. No re-dragging windows. No fiddling with the display panel. No "wait, which one was 144Hz again?"

## How it works

You start where you always have: the Windows display manager. Arrange your monitors, dial in the resolutions and refresh rates, and get everything looking exactly the way you want it.

Then you snapshot it.

Click the system tray icon and hit **Save**, or drop into the console and use `mmcli`:

```
mmcli -save "Desktop Configuration"
```

That's it. Your layout is now a named profile, frozen in time and ready whenever you want it.

Later—maybe hours later, maybe after three reboots and a GPU driver update—you **recall** it. Click the tray icon and pick your profile, or run:

```
mmcli -load "Desktop Configuration"
```

Every display snaps back into place. Same arrangement, same resolutions, same refresh rates. Like it never moved.

## Two ways to drive it

Some people want a quick click. Some people want a keyboard and a command line. Multi-Monitor Profiles serves both without compromise.

The **system tray icon** keeps your profiles one click away—ideal for the couch-to-desk-to-couch shuffle.

The **`mmcli` command-line tool** gives you full control and scriptability. Wire it into batch files, hook it to a launcher, trigger it from a stream deck, or chain it into your "start work" routine. Anything that can run a command can switch your monitors.

```
Usage:
  mmcli -save <profile_name>    Save current monitor configuration
  mmcli -load <profile_name>    Load and apply saved configuration
  mmcli -list                   List all saved profiles
  mmcli -delete <profile_name>  Delete a saved profile
  mmcli -show                   Show current monitor configuration
  mmcli -help                   Show this help message
```

## Built for the way you actually use your setup

- **Desktop mode** — Triple monitors, productivity layout, everything where your muscle memory expects it.
- **Couch mode** — TV solo, big and clean, the rest powered down and out of the way.
- **Game mode, present mode, whatever-mode** — If you can arrange it, you can save it. Name it. Recall it.

Switch contexts in a second, not a sigh.

---

**Multi-Monitor Profiles.** Your displays, exactly how you left them—because you told them to stay that way.
