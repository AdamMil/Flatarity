Flatarity is a clone of the game at http://www.planarity.net, which was
written by John Tantalo.

This version adds features that I thought were lacking, although it's missing
some of the polish that the Flash version has. This version requires the
Microsoft .NET framework version 2.0, which can be obtained from:
http://www.microsoft.com/downloads/details.aspx?FamilyID=0856EACB-4362-4B0D-8EDD-AAB15C5E04F5

Features:
* Selecting and moving multiple vertices
* Flipping and rotating vertices
* Zooming and panning the display
* Snapping vertices to a grid
* Automatic saving and loading of your game and settings
* Resizable window
* Higher performance (via poorer graphics)

Directions:
* Left-drag in empty space to start selecting multiple vertices.
* Ctrl-click a vertex to toggle whether it's selected.
* Ctrl-left-drag to toggle the selection of multiple vertices.
* Alt-left-drag to rotate selected vertices around the mouse point.
* Right-drag to pan around.
* Mouse wheel to zoom in and out.
* Press 'H' to horizontally flip the selected vertices around their center.
* Press 'V' to vertically flip the selected vertices around their center.
* Press 'R' to reset the viewpoint and mark vertices unmoved.
* Press 'M' to mark vertices moved.
* Press 'G' to change the grid size or turn it off.
* Press 'S' to snap selected vertices to the grid.
* Press 'C' to select the vertices connected to the vertex under the mouse.
  This is useful at very high levels where may be difficult to select the
  vertex you want.
* Press 'P' to pause.
* Press Enter to check your solution.

Credit:
Thanks to John Tantalo for the cool game, the inspiration, and the algorithm
for generating planar graphs, which I got from here:
http://www.johntantalo.com/wiki/Planarity

Thanks to Microsoft for the .NET platform.