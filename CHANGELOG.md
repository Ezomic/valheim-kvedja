# Changelog

## 1.0.0 - pending

First release, and the first mod in the suite whose job is to point at something outside the
game.

When you appear in the world it reads
[longhouse.thijssensoftware.nl/api/motd](https://longhouse.thijssensoftware.nl/api/motd) and
prints what it says into the chat window, one line per line, under a name in orange. Then it
stops. It does not repeat, it does not follow you around, and it says nothing at all if the
site has nothing to say. By default it stays quiet when you respawn after dying.

**The text is not in the DLL.** It lives on the site and is edited there, so what it says can
change without an update to install and without a server restart, and the same line reaches you
in singleplayer, on Longhouse and on somebody else's server. Point `Url` somewhere else and this
is a message of the day for any server that wants one; the only requirement at the far end is
that the answer is plain text.

Settled since 0.1.0, and none of it was readable without running it:

- **The request works from inside the game.** The TLS line was copied from Crier, which makes
  this trip from a dedicated server, and a client is a different process - so it was a guess
  until the scenario watched a real fetch land.
- **The timeout is a `CancellationTokenSource`, not `HttpClient.Timeout`.** Setting the property
  per request throws `InvalidOperationException` the second time, because the handler is started
  by the first one - so the first login of a session would have worked and every later one would
  have failed with something that reads like a network fault.
- **The line arrives under its title.** `kvedja-greets-you-in-chat` reads the scrollback for both
  the speaker and the address, which separates "the message arrived" from "the message arrived
  without its name on it".

Two things the scenario deliberately does not cover, because a replayed scenario starts after
the moment they happen, and both are worth watching by eye the first time:

- **Whether the chat window opens by itself.** Adding a line to the buffer does not do it.
  `Chat.Update` draws the window only while its hide timer is under the delay, and it is the
  incoming-message path that resets that timer, not `AddString`. Kvedja reaches the timer by
  reflection and zeroes it. If that binding is ever wrong, the message is in the scrollback and
  invisible - which is the failure to look for first, because everything else reports success.
- **Whether four seconds is the right wait.** The delay exists because the first second or two
  after spawning is the loading screen fading out, and a line printed under it has been said to
  nobody. The number is still a guess at the length of a fade.

It registers with Core's gate at `Requirement.HostOnly` and marks every setting as yours. A host
does not get to point your client at an address of its choosing - Core writes an imposed value
into the guest's own cfg, so it would still be there after they disconnected, and a greeting is
not worth that.
