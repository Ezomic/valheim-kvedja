# Changelog

## 0.1.0 - unreleased

First version. Builds and deploys; **never run in game**.

Reads the message from the site when you log in and prints it into the chat window. The text
is not in the DLL, so it can be changed without an update and it arrives in singleplayer as
well as on a server.

Unverified until it is run:

- **Whether the chat window opens by itself.** Adding a line to the chat buffer does not do
  it. `Chat.Update` draws the window only while its hide timer is under the delay, and it is
  the incoming-message path that resets that timer, not `AddString`. Kvedja reaches the timer
  by reflection and zeroes it. If that binding is ever wrong the message is still in the
  scrollback and invisible, which is the failure worth looking for first.
- **Whether four seconds is the right wait.** The delay exists because the first second or two
  after spawning is the loading screen fading out, and a line printed under it has been said
  to nobody. The number is a guess at the length of a fade.
- **The request itself**, end to end, from inside the game process against the live site. The
  TLS line is copied from Crier, which does make this trip successfully from a server, but a
  client is a different process and has not been watched doing it.
