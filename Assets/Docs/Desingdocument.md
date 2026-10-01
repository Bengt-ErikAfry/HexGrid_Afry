\# Space Hex Strategy Game

Is a super simpel turn based strategy game for mobile phones.

Focus on simple code and easy to understand for developer.

Chose simpel but maybe more performance heavy solution before a high performant solution that is hard to understand.

 

\## Architecture

 

Single Gameplay Scene

 

Views:

\- Galaxy View

\- Asteroid View

\- Interior View

 

\## Persistence

 

Template JSON

\- Ship layouts

\- Asteroid layouts

\- Galaxy layouts

 

Instance JSON

\- Fog of war

\- Unit positions

\- Door states

\- Ore remaining

\- Enemy states

 

\## Turn System

 

Strategic Turns:

\- Galaxy

\- Asteroids

\- Mining

\- Fleet movement

 

Combat Turns:

\- Boarding

\- Outpost combat

 

Combat turns do not advance strategic turns.

 

\## Hex Editor

 

Editor tool that:

\- Displays ghost hex grid

\- Click places tile prefab

\- Shift-click removes tile

\- Exports JSON templates2



\## Hex grid

The hex grid are only used for editor placement of tiles.

In game i use tiles for highlighting, navigation.

