# Kvedja

A line in the chat window when you log in, read from the Longhouse site.

## Why

The greeting is not the point. The point is that there is a board where players vote on what
a mod should do next, and a board for bugs, and almost nobody knows either exists.

A link in a README is read once, by the person installing, months before they have an opinion
worth casting. A pinned message in Discord is read by the people who are already in Discord.
The moment that actually matters is when somebody is in the game, having just been annoyed by
something or having just thought of something they wish a mod did, and that is the moment the
address is worth having in front of them. The chat window is where they are already looking.

*Kveðja* is Old Norse for a greeting.

## What it does

When you appear in the world, it reads
[longhouse.thijssensoftware.nl/api/motd](https://longhouse.thijssensoftware.nl/api/motd) and
prints what it says into the chat window, one line per line, under a name in orange. Then it
stops. It does not repeat, it does not follow you around, and it says nothing at all if the
site has nothing to say.

By default it stays quiet when you respawn after dying, because being greeted at the moment
you have just lost everything reads as mockery. There is a setting if you disagree.

## The text is not in the mod

Which is the design, and worth saying plainly before anybody worries about it.

The message lives on the site and the client reads it when you log in. That means it can be
changed without an update to install and without a server restart, and it means the same line
reaches you in singleplayer, on Longhouse, and on somebody else's server. It also means this
mod makes one small web request per login, to that address and no other, and sends nothing
about you with it.

Point `Url` somewhere else and this is a message of the day for any server that wants one. The
only requirement at the far end is that the answer is plain text.

If the site is unreachable, the request fails and nothing is said. That is deliberate: you
cannot fix a site being down, so a greeting that complains about itself is worse than no
greeting.

## Why it is a client mod and not a server one

Worth writing down because the obvious expectation is the opposite.

A Valheim server cannot put a line in the chat window. The route that looks like it should
work, sending a chat message over the network, ends in the client looking the sender up among
the connected players, and a dedicated server has no entry in that list. Every attempt
produces an error in the log and a floating piece of world text at the centre of the map. The
call that does work takes a plain name instead of a player, does no lookup, and has no network
path behind it, so it can only be made on the machine that is reading the message.

That is also why a server cannot silence this one, and why every setting here is yours.

## Installing

Needs BepInEx. Nothing else. Through a mod manager it is one install. By hand, put
`Kvedja.dll` in `BepInEx/plugins/Kvedja/`.

Then start the game once and quit. That first run writes the config file. It does not exist
before the mod has loaded, which is the usual reason people think it is broken.

## Settings

The file is `BepInEx/config/ezomic.valheim.kvedja.cfg`. Open it in any text editor. Every
setting has a comment above it, so the file explains itself.

Note that changing a default in a new version does nothing on a machine that has already run
the mod. BepInEx writes every entry on first run and the saved value wins.

## Multiplayer

**Nobody else needs it.** It reads a web page and writes into your own chat log. A server does
not know it is there, and neither does anybody playing beside you.

If [Core](https://github.com/Ezomic/valheim-core) is installed, this mod registers with its
version gate so a mismatch is reported rather than discovered later. Every setting is marked
as yours, the address included. A host imposing that one would be pointing your client at an
address of its choosing, and it would still be in your config after you disconnected.

## Bugs and ideas

Both go to the site. [longhouse.thijssensoftware.nl/bugs](https://longhouse.thijssensoftware.nl/bugs)
is for anything broken, and [longhouse.thijssensoftware.nl/ideas](https://longhouse.thijssensoftware.nl/ideas)
is for what a mod should do next. You can vote on other people's ideas there as well.

Signing in takes a Steam or Discord account. I work from that list, so the votes decide what
I pick up next.

## Licence

MIT. See `LICENSE`.
