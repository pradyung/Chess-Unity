# Chess

## Description
This is a simple chess game I made in Unity that can be played by two players over the Internet or on a single computer. The game operates on a create/join game system, where one player creates a game and gets a join code, which the other player can then use to join the game. The game can be downloaded and played at [https://pradyung.itch.io/chess](https://pradyung.itch.io/chess).

<img width="446" alt="image" src="https://github.com/pradyung/Chess-Unity/assets/103707604/ea4ffdd1-9a90-4eb5-891d-b59b465bf46b">


## How to Play
There are two ways to play the game:
* Side-By-Side: This mode allows two players to play on a single device. To play side by side, click Start Game and then Side by Side.
* Multiplayer: This mode allows two players to play over the Internet. To play multiplayer, click Start Game and then Multiplayer. Now, click Create Game. This will give you a join code which you can send to another player. They will then have to click Join Game instead of Create Game and enter in the join code.

## How it Works
The multiplayer side of the game works using Unity Netcode to create a connection between the host and the client, and Unity Relay to allow cross-network gameplay. All of the logic is written in C#. I have plans to implement a chess engine in the future to allow for human vs. computer gameplay.
