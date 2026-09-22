# Changelog

## 1.0.0 - pending

First release.

When you appear in the world it reads
[longhouse.thijssensoftware.nl/api/motd](https://longhouse.thijssensoftware.nl/api/motd) and
prints what it says into the chat window, one line per line, under a name in orange. Then it
stops. It does not repeat, it does not follow you around, and it says nothing at all if the
site has nothing to say. By default it stays quiet when you respawn after dying.

**The text is not in the DLL.** It lives on the site and is edited there, so it changes without
an update to install or a server restart, and the same line reaches you in singleplayer, on
Longhouse and on somebody else's server. Point `Url` somewhere else and it is a message of the
day for any server that wants one, as long as the far end answers plain text.

Settled since 0.1.0 by running it:

- The request works from inside the game. The TLS line came from Crier, which makes the trip
  from a dedicated server, and a client is a different process, so it was a guess until the
  scenario watched a real fetch land.
- The timeout is a `CancellationTokenSource`, not `HttpClient.Timeout`. Setting that property
  per request throws `InvalidOperationException` the second time, because the first request
  starts the handler. The first login of a session would have worked and every later one would
  have failed looking like a network fault.
- The line arrives under its title. `kvedja-greets-you-in-chat` reads the scrollback for the
  speaker as well as the address, which tells "the message arrived" apart from "the message
  arrived without its name on it".

Two things the scenario cannot cover, since a replay starts after both have happened. Watch
them by eye the first time:

- Whether the chat window opens by itself. Adding a line to the buffer does not do it.
  `Chat.Update` draws the window only while its hide timer is under the delay, and the
  incoming-message path resets that timer, not `AddString`. Kvedja reaches the timer by
  reflection and zeroes it. If that binding is ever wrong the message sits in the scrollback,
  invisible, with everything else reporting success.
- Whether four seconds is the right wait. The first second or two after spawning is the loading
  screen fading out, and a line printed under it has been said to nobody. The number is a guess
  at the length of a fade.

Registers with Core's gate at `Requirement.HostOnly` and marks every setting as yours. A host
imposing `Url` would point your client at an address of its choosing, and Core writes an
imposed value into the guest's own cfg, so it would still be there after they disconnected.
