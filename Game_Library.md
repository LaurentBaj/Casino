# Casino.Core

<hr>

This is a game library for the card game Casino. It consists of pure functions that modify and return an updated game state
based on natural events that happen in the game. Its goal is to be total frontend agnostic and should aid the developer with
creating the revered game with great easy and correctness.


### Events and handling

1. Game initialization ✔️
2. Loop
      - Deal cards (2-4 players + table if first round) ✔️
      - Player turn + action LOOP
              - Place ✔️
              - Merge / Build
              - Collect ✔️
          - repeat until hands are empty
      - If last_round then
          BREAK;
        else Loop
    
3. Provide game details (winner, points etc..)

 

