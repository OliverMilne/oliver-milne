This is a hobby project with the goal of building a little 4X game - keeping things nice and simple mechanically, just getting to know Unity's various features. Right now I've been working on it in my spare time for maybe a month, so it's barely begun - it's got simple placeholder map gen, combat, simple AI (a group of three enemies roams around together and attacks your units if they see them), and a working save-load system (if you have a D drive, or manually change the save file path in the code!). 

Arrow keys move the camera. Debug controls are as follows:
Space - toggle sprite visibility for the selected object
B - apply 1-turn movement buff to selected unit
C - spawn unit for human player at mouse location
D - damage selected unit for 1hp
L - load game
N - new game
O - cleanly destroy all LocatableObjects
R - render stat bar background on selected unit (shouldn't actually do anything as that's working now)
S - save game
V - show how many players can see each tile
X - show VisibleUnitID on each tile (renders underneath units - bug to be fixed)
Y - some abstruse debug info display from when I was sorting out showing unit sprites properly
Z - toggle fog of war