/*
This is a clone of the game at http://www.planarity.net
Copyright (C) 2007 Adam Milazzo (http://www.adammil.net/)

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Collections.Generic;
using System.IO;
using GameLib;
using GameLib.Events;
using GameLib.Fonts;
using GameLib.Input;
using GameLib.Mathematics;
using GameLib.Mathematics.TwoD;
using GameLib.Video;
using BinaryReader = AdamMil.IO.BinaryReader;
using BinaryWriter = AdamMil.IO.BinaryWriter;
using Color = System.Drawing.Color;
using Debug = System.Diagnostics.Debug;
using SPoint=System.Drawing.Point;

namespace Flatarity
{

static class Game
{
  const int MaxGridSize = 32;
  const int SaveVersion = 1;

  static void Main()
  {
    Events.Initialize();
    Input.Initialize();
    Video.Initialize();

    Video.SetMode(800, 600, 0, SurfaceFlag.Resizeable | SurfaceFlag.DoubleBuffer);
    WM.WindowTitle = "Flatarity by Adam Milazzo (a clone of http://www.planarity.net/)";

    string fontFile = Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "Fonts/arial.ttf");
    font = new TrueTypeFont(fontFile, 18);
    font.Style = FontStyle.Bold;
    font.RenderStyle = RenderStyle.Blended;

    LoadGame();
    Events.PumpEvents(EventProc, IdleProc);
    SaveGame();

    Video.Deinitialize();
    Input.Deinitialize();
    Events.Deinitialize();
  }

  static bool IsRotating
  {
    get { return Keyboard.HasOnly(KeyMod.Alt) && savedPoints != null; }
  }

  static void GenerateLevel(int level)
  {
    Debug.Assert(level >= 1);
    
    // we'll implement the same algorithm as Planarity.
    // http://www.johntantalo.com/wiki/Planarity

    // first generate N random lines where none are parallel, where N == 3+level
    Line[] lines = new Line[3+level];
    for(int i=0; i<lines.Length; i++)
    {
      while(true)
      {
        lines[i] = new Line(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
        bool isParallel = false;
        for(int j=0; j<i; j++)
        {
          if(!Math2D.Intersects(ref lines[i], ref lines[j]))
          {
            isParallel = true;
            break;
          }
        }

        if(!isParallel) break;
      }
    }

    // this makes for N*(N-1)/2 vertices, because each line intersects N-1 other lines, and each pair forms a vertex
    Vertices = new Vertex[lines.Length*(lines.Length-1)/2];

    // there are N*(N-2) edges, as each line intersects N-1 other lines, forming N-2 segments
    Connections = new Connection[lines.Length * (lines.Length-2)];
    // we know that each line intersects every other line, but we don't know the order of the intersects. without
    // knowing the order, we can't tell which edges connect which vertices.
    int[] lineIndices = new int[lines.Length-1];
    for(int li=0,ci=0; li<lines.Length; li++) // so for each line
    {
      // copy the indices of the lines intersected by this line (ie, all lines except this one) into an array
      for(int i=0; i<lineIndices.Length; i++) lineIndices[i] = i<li ? i : i+1;
      // then sort the lines based on the order of the intersection points
      Array.Sort(lineIndices, new LineComparer(lines, li));
      for(int i=0; i<lineIndices.Length-1; ci++,i++) // add each edge created by this line
      {
        Connections[ci] = new Connection(PairIndex(li, lineIndices[i], lines.Length),
                                         PairIndex(li, lineIndices[i+1], lines.Length));
      }
    }

    // place the vertices in a circle
    double angleMul = Math.PI*2 / Vertices.Length;
    for(int i=0; i<Vertices.Length; i++)
    {
      Vertices[i].Position = new Vector(0, -0.90).Rotated(angleMul*i).ToPoint();
    }

    selected.Clear();
    buttonOver  = buttonPress = dragVertex = highlighted = -1;
    Game.level  = level;
    ResetViewpoint();
    Timing.Reset();
    timeOffset = 0;
  }

  /// <summary>Gets the center point of the selected vertices.</summary>
  static Point GetSelectedCenter()
  {
    Point topLeft = new Point(double.MaxValue, double.MaxValue), // find the extent of the selected vertices
      bottomRight = new Point(double.MinValue, double.MinValue);
    foreach(int vertex in selected)
    {
      Point pos = Vertices[vertex].Position;
      if(pos.X < topLeft.X) topLeft.X = pos.X;
      if(pos.X > bottomRight.X) bottomRight.X = pos.X;
      if(pos.Y < topLeft.Y) topLeft.Y = pos.Y;
      if(pos.Y > bottomRight.Y) bottomRight.Y = pos.Y;
    }

    return topLeft + (bottomRight-topLeft)*0.5; // find the center point of the selected vertices
  }

  static void HorizontalFlip()
  {
    Point centerPt = GetSelectedCenter();
    foreach(int vertex in selected)
    {
      Vertices[vertex].Position.X += (centerPt.X-Vertices[vertex].Position.X)*2;
    }
    doRepaint = true;
  }

  static void VerticalFlip()
  {
    Point centerPt = GetSelectedCenter();
    foreach(int vertex in selected)
    {
      Vertices[vertex].Position.Y += (centerPt.Y-Vertices[vertex].Position.Y)*2;
    }
    doRepaint = true;
  }

  static void LoadGame()
  {
    GenerateLevel(1);

    string saveFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "Flatarity/save.dat");
    if(File.Exists(saveFile))
    {
      try
      {
        using(FileStream stream = new FileStream(saveFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        using(BinaryReader br = new BinaryReader(stream))
        {
          if(br.ReadInt32() == SaveVersion)
          {
            Vertices = new Vertex[br.ReadInt32()];
            for(int i=0; i<Vertices.Length; i++) Vertices[i] = new Vertex(br);
            Connections = new Connection[br.ReadInt32()];
            for(int i=0; i<Connections.Length; i++) Connections[i] = new Connection(br);
            selected.AddRange(br.ReadInt32(br.ReadInt32()));
            cameraPoint = new Point(br.ReadDouble(), br.ReadDouble());
            zoomLevel   = br.ReadDouble();
            timeOffset  = br.ReadDouble();
            score       = br.ReadInt32();
            level       = br.ReadInt32();
            gridSize    = br.ReadInt32();
          }

          Timing.Reset();
        }
      }
      catch { GenerateLevel(1); }
    }
  }

  static void MarkMoved(bool moved)
  {
    if(selected.Count == 0)
    {
      for(int i=0; i<Vertices.Length; i++) Vertices[i].Moved = moved;
    }
    else
    {
      foreach(int vertex in selected) Vertices[vertex].Moved = moved;
    }
    doRepaint = true;
  }

  static int PairIndex(int p, int q, int n)
  {
    Debug.Assert(p != q);
    if(p > q) { int t = p; p = q; q = t; } // ensure that p < q
    return (2*n-p-1)*p/2+q-p-1;
  }

  static void PaintScreen()
  {
    Video.DisplaySurface.Fill(Color.White);

    if(gridSize != 0)
    {
      Color gridColor = Color.FromArgb(248, 248, 248);
      for(int y=0,width=Video.Width-1; y<Video.Height; y += gridSize)
      {
        Primitives.HLine(Video.DisplaySurface, 0, width, y, gridColor);
      }
      for(int x=0,height=Video.Height-1; x<Video.Width; x += gridSize)
      {
        Primitives.VLine(Video.DisplaySurface, x, 0, height, gridColor);
      }
    }

    // render UI buttons
    for(int i=0; i<Buttons.Length; i++) Buttons[i].Render(i);

    // render level and score display
    font.BackColor = Color.White;
    font.Color     = Color.DarkGray;
    font.Render(Video.DisplaySurface, "Score: "+score, 5, 5);
    font.Render(Video.DisplaySurface, "Level: "+level, 5, 5+font.LineSkip);

    // reset per-vertex render state
    for(int i=0; i<Vertices.Length; i++) Vertices[i].ConnectedToHighlight = false;

    // render the connections
    for(int i=0; i<Connections.Length; i++)
    {
      Connection conn = Connections[i];
      Vertex from = Vertices[conn.First], to = Vertices[conn.Second];

      Color lineColor = conn.Failed            ? Color.Red   :
                        from.Moved && to.Moved ? Color.Black :
                        from.Moved || to.Moved ? Color.DimGray : Color.DarkGray;
      Primitives.LineAA(Video.DisplaySurface, from.GetScreenPoint(), to.GetScreenPoint(), lineColor);

      if(conn.First == highlighted) Vertices[conn.Second].ConnectedToHighlight = true;
      else if(conn.Second == highlighted) Vertices[conn.First].ConnectedToHighlight = true;
    }
    
    Rectangle selectionRect;
    bool selecting = dragButton == MouseButton.Left && dragVertex == -1 && buttonPress == -1 && !IsRotating;
    if(selecting) selectionRect = new Rectangle(mouseDownPoint, ToVirtualPoint(Mouse.Point));
    else selectionRect = new Rectangle();

    // render the vertices
    for(int i=0; i<Vertices.Length; i++)
    {
      Vertex v = Vertices[i];
      SPoint pt = v.GetScreenPoint();

      Color vertexColor = selecting && selectionRect.Contains(v.Position) || selected.Contains(i) ? Color.Green :
                          v.ConnectedToHighlight && !selecting ? Color.Red   :
                          highlighted == i ? Color.White : Color.Blue;
      Primitives.FilledCircle(Video.DisplaySurface, pt, v.Radius, vertexColor);
      Primitives.CircleAA(Video.DisplaySurface, pt, v.Radius, Color.Black);
      Primitives.CircleAA(Video.DisplaySurface, pt, v.Radius+1, Color.Black);
    }

    // render the selection box
    if(selecting)
    {
      SPoint corner1 = ToScreenPoint(mouseDownPoint), corner2 = Mouse.Point;
      Primitives.Box(Video.DisplaySurface, corner1, corner2, Color.DarkCyan);
    }

    if(paused)
    {
      Primitives.FilledBox(Video.DisplaySurface, Video.DisplaySurface.Bounds, Color.White, 192);
      font.Color = Color.DimGray;
      font.Center(Video.DisplaySurface, "Paused. Click to continue.");
    }

    Video.Flip();
  }

  static void ResetViewpoint()
  {
    cameraPoint = new Point();
    zoomLevel   = 1;
    doRepaint   = true;
  }

  static bool EventProc(Event e)
  {
    const double ZoomSpeed = 0.75;
    
    Input.ProcessEvent(e);

    switch(e.Type)
    {
      case EventType.Keyboard:
      {
        KeyboardEvent ke = (KeyboardEvent)e;

        if(ke.Down)
        {
          char c = char.ToUpperInvariant(ke.Char);

          if(ke.Key == Key.F4 && ke.HasOnly(KeyMod.Alt))
          {
            return false; // alt-f4 quits the program
          }
          else if(c == 'H')
          {
            HorizontalFlip();
          }
          else if(c == 'V')
          {
            VerticalFlip();
          }
          else if(c == 'R')
          {
            ResetViewpoint();
            MarkMoved(false);
          }
          else if(c == 'M') // mark all vertices as moved
          {
            MarkMoved(true);
          }
          else if(c == 'G') // toggle grid
          {
            if(gridSize == 0) gridSize = MaxGridSize;
            else
            {
              gridSize /= 2;
              if(gridSize < 16) gridSize = 0;
            }
            doRepaint = true;
          }
          else if(c == 'S') // snap selected points to grid
          {
            if(gridSize != 0)
            {
              foreach(int vertex in selected)
              {
                Vertices[vertex].Position = ToVirtualPoint(SnapToGrid(ToScreenPoint(Vertices[vertex].Position)));
              }
              doRepaint = true;
            }
          }
          else if(c == 'C') // select points connected to hovered point
          {
            if(highlighted != -1)
            {
              selected.Clear();
              foreach(Connection conn in Connections)
              {
                if(conn.First == highlighted) SelectVertex(conn.Second);
                else if(conn.Second == highlighted) SelectVertex(conn.First);
              }
              doRepaint = true;
            }
          }
          else if(c == 'P')
          {
            Pause();
          }
        }
        else if(Keyboard.IsModKey(ke.Key) && dragButton == MouseButton.Left)
        {
          if(dragVertex == -1 && buttonPress == -1 &&
             savedPoints != null && (ke.Key == Key.LAlt || ke.Key == Key.RAlt)) // undo rotation
          {
            for(int i=0; i<savedPoints.Length; i++) Vertices[selected[i]].Position = savedPoints[i];
            FinishDrag();
          }
        }

        break;
      }
      
      case EventType.MouseClick:
      {
        MouseClickEvent ce = (MouseClickEvent)e;

        // if we were dragging something and released it...
        if(dragButton != NotDragging && !ce.Down && ce.Button == dragButton)
        {
          FinishDrag();
        }
        else if(ce.Down && dragButton == NotDragging) // otherwise, if a button was pressed and we're not currently
        {                                             // dragging anything
          if(ce.Button == MouseButton.Left)
          {
            mouseDownPoint = ToVirtualPoint(Mouse.Point);

            // first see if the user is clicking a vertex
            UpdateHighlights();       // update the highlights so we know if the mouse pointer is over a vertex
            dragVertex = highlighted; // drag the vertex under the mouse

            if(Keyboard.HasOnly(KeyMod.Alt)) // alt-click rotates the selected points
            {
              savedPoints = new Point[selected.Count]; // save the original positions of the selected points
              for(int i=0; i<savedPoints.Length; i++) savedPoints[i] = Vertices[selected[i]].Position;
              dragVertex = -1; // we're not dragging this vertex
              dragButton = ce.Button;
            }
            else if(dragVertex != -1)
            {
              if(Keyboard.HasOnly(KeyMod.Ctrl)) // a ctrl-click toggles the selection of a vertex
              {
                if(selected.Contains(dragVertex)) selected.Remove(dragVertex);
                else selected.Add(dragVertex);
              }
              // if the clicked vertex was not selected, deselect all vertices
              else if(!selected.Contains(dragVertex)) selected.Clear();
            }
            else // a vertex was not clicked. check the UI buttons
            {
              UpdateButtonOver();
              buttonPress = buttonOver;
              if(buttonPress == -1) dragButton = ce.Button; // if no UI button was pressed, start a box selection
            }

            if(dragVertex != -1 || buttonPress != -1)
            {
              dragButton = ce.Button;
              doRepaint  = true;
            }
          }
          else if(ce.Button == MouseButton.Right)
          {
            dragButton     = ce.Button;
            dragPoint      = cameraPoint;
            mouseDownPoint = ToVirtualPoint(Mouse.Point);
          }
          else if(ce.Button == MouseButton.WheelDown)
          {
            zoomLevel *= ZoomSpeed;
            doRepaint  = true;
          }
          else if(ce.Button == MouseButton.WheelUp)
          {
            zoomLevel *= 1/ZoomSpeed;
            doRepaint  = true;
          }
        }
        break;
      }

      case EventType.MouseMove:
        if(dragButton == NotDragging)
        {
          UpdateHighlights();
          if(highlighted != -1) buttonOver = buttonPress = -1;
          else UpdateButtonOver();
        }
        else if(dragButton == MouseButton.Left)
        {
          if(dragVertex != -1)
          {
            Point currentPoint = ToVirtualPoint(SnapToGrid(Mouse.Point));
            if(selected.Count == 0)
            {
              Vertices[dragVertex].Moved    = true;
              Vertices[dragVertex].Position = currentPoint;
            }
            else
            {
              Vector movement = currentPoint - mouseDownPoint;
              foreach(int vertex in selected)
              {
                Vertices[vertex].Position += movement;
              }
              mouseDownPoint = currentPoint;
            }
          }
          else if(buttonPress != -1)
          {
            UpdateButtonOver();
          }
          else if(IsRotating) // rotate around the mouse down point
          {
            const int RotationsPerScreen = 4;

            SPoint originalPoint = ToScreenPoint(mouseDownPoint);
            int distance = (Mouse.X - originalPoint.X) + (originalPoint.Y - Mouse.Y);
            double angle = distance * (Math.PI*2) / (Video.Width/RotationsPerScreen);

            // if the distance is negative, we rotate left. otherwise, we rotate right.
            for(int i=0; i<savedPoints.Length; i++)
            {
              int vertex = selected[i];
              Vertices[vertex].Position = (savedPoints[i]-mouseDownPoint).Rotated(angle) + mouseDownPoint;
            }
          }

          doRepaint = true;
        }
        else if(dragButton == MouseButton.Right)
        {
          // reset the camera point so we have our original perspective when calculating the distance moved
          cameraPoint = dragPoint;
          cameraPoint = dragPoint - (ToVirtualPoint(Mouse.Point) - mouseDownPoint);
          doRepaint   = true;
        }
        break;

      case EventType.Resize:
      {
        ResizeEvent re = (ResizeEvent)e;
        Video.SetMode(re.Width, re.Height, 0, SurfaceFlag.Resizeable | SurfaceFlag.DoubleBuffer);
        break;
      }
      
      case EventType.Repaint:
        doRepaint = true;
        break;

      case EventType.Quit:
        return false;
    }

    return true;
  }

  static void FinishDrag()
  {
    UpdateButtonOver();
    if(dragButton == MouseButton.Left)
    {
      // if a UI button was clicked, activate the handler
      if(buttonPress != -1 && Buttons[buttonPress].Contains(Mouse.Point))
      {
        Buttons[buttonPress].Handler();
      }
      else if(dragVertex != -1) // if we were dragging a vertex, stop it
      {
        if(selected.Count != 0)
        {
          ClearSelectedFailureLines();
        }
        else
        {
          for(int i=0; i<Connections.Length; i++) // reset any 'failed' flags that correspond to this vertex
          {
            if(Connections[i].Hasvertex(dragVertex)) Connections[i].Failed = false;
          }
        }
        dragVertex = -1;
      }
      else if(dragVertex == -1 && !Keyboard.HasAny(KeyMod.Alt)) // we were selecting points
      {
        Rectangle selectionRect = new Rectangle(mouseDownPoint, ToVirtualPoint(Mouse.Point));

        bool toggling = Keyboard.HasOnly(KeyMod.Ctrl);
        if(!toggling) selected.Clear();

        for(int i=0; i<Vertices.Length; i++)
        {
          if(selectionRect.Contains(Vertices[i].Position))
          {
            if(toggling && selected.Contains(i)) selected.Remove(i);
            else selected.Add(i);
          }
        }

        doRepaint = true; // erase the box
      }

      if(buttonPress != -1) // reset the pressed button
      {
        doRepaint = true;
        buttonPress = -1;
      }

      savedPoints = null; // reset the saved point list
    }

    dragButton = NotDragging; // in any case, we're no longer dragging
  }

  static bool IdleProc()
  {
    if(doRepaint)
    {
      PaintScreen();
      doRepaint = false;
    }
    return false;
  }

  static void CheckSolution()
  {
    double elapsedTime = Timing.Seconds + timeOffset;
    
    bool failed = false;
    for(int i=0; i<Connections.Length; i++)
    {
      Connection iConn = Connections[i];
      Line iLine = iConn.GetLine();
      for(int j=i+1; j<Connections.Length; j++)
      {
        // skip lines that share a vertex
        if(iConn.Hasvertex(Connections[j].First) || iConn.Hasvertex(Connections[j].Second)) continue;

        if(iLine.SegmentIntersects(Connections[j].GetLine()))
        {
          Connections[i].Failed = Connections[j].Failed = failed = true;
          goto done;
        }
      }
    }

    done:
    if(failed) doRepaint = true;
    else
    {
      score += Math.Max(0, 100*level - (int)Math.Round(elapsedTime));
      GenerateLevel(level+1);
    }
  }

  static void ClearSelectedFailureLines()
  {
    for(int i=0; i<Connections.Length; i++) // reset any 'failed' flags that correspond to edges of selected vertices
    {
      if(selected.Count != 0)
      {
        foreach(int vertex in selected)
        {
          if(Connections[i].Hasvertex(vertex))
          {
            Connections[i].Failed = false;
            break;
          }
        }
      }
    }
  }

  static void Pause()
  {
    double startTime = Timing.Seconds;
    paused = true;
    PaintScreen();
    
    Event e;
    while(true)
    {
      e = Events.NextEvent();
      Input.ProcessEvent(e);
      if(e.Type == EventType.Quit || e.Type == EventType.Resize)
      {
        if(!EventProc(e)) break;
      }
      else if(e.Type == EventType.Repaint)
      {
        PaintScreen();
      }
      else if(e.Type == EventType.Keyboard)
      {
        KeyboardEvent ke = (KeyboardEvent)e;
        if(ke.Key == Key.F4 && ke.HasOnly(KeyMod.Alt))
        {
          Events.QuitFlag = true;
          break;
        }
      }
      else if(e.Type == EventType.MouseClick)
      {
        MouseClickEvent ce = (MouseClickEvent)e;
        if(ce.Down && ce.Button == MouseButton.Left) break;
      }
    }

    timeOffset -= Timing.Seconds-startTime;
    paused      = false;
    doRepaint   = true;
  }

  static void SaveGame()
  {
    try
    {
      string saveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Flatarity");
      Directory.CreateDirectory(saveDir);
    
      using(FileStream stream = new FileStream(Path.Combine(saveDir, "save.dat"), FileMode.Create, FileAccess.Write))
      using(BinaryWriter bw = new BinaryWriter(stream))
      {
        bw.Write(SaveVersion);
        bw.Write(Vertices.Length);
        for(int i=0; i<Vertices.Length; i++) Vertices[i].Write(bw);
        bw.Write(Connections.Length);
        for(int i=0; i<Connections.Length; i++) Connections[i].Write(bw);
        bw.Write(selected.Count);
        bw.Write(selected.ToArray());
        bw.Write(cameraPoint.X);
        bw.Write(cameraPoint.Y);
        bw.Write(zoomLevel);
        bw.Write(timeOffset + Timing.Seconds);
        bw.Write(score);
        bw.Write(level);
        bw.Write(gridSize);
      }
    }
    catch { }
  }

  static void SelectAll()
  {
    selected.Clear();
    for(int i=0; i<Vertices.Length; i++) selected.Add(i);
    doRepaint = true;
  }

  static void SelectVertex(int index)
  {
    Debug.Assert(index >= 0 && index < Vertices.Length);
    int selIndex = selected.IndexOf(index);
    if(selIndex == -1) selected.Add(index);
    else selected.RemoveAt(selIndex);
  }

  static void ShowHelp()
  {
  }

  static void ShuffleVertices()
  {
    for(int i=0; i<Vertices.Length; i++)
    {
      Vertices[i].Position = new Point(rand.NextDouble()*2-1, rand.NextDouble()*2-1);
      Vertices[i].Moved    = false;
    }
    doRepaint = true;
  }

  static void SkipToLevel()
  {
    LevelForm form = new LevelForm();
    form.Level = level;
    if(form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    {
      score = 0;
      GenerateLevel(Math.Max(1, form.Level));
    }
  }

  static SPoint SnapToGrid(SPoint pt)
  {
    if(gridSize > 0)
    {
      // we know gridSize is a power of two so we can subtract one to get a mask
      int halfGrid = gridSize/2, gridMask = gridSize-1, xd = pt.X & gridMask, yd = pt.Y & gridMask;
      pt.X &= ~gridMask;
      if(xd >= halfGrid) pt.X += gridSize;
      pt.Y &= ~gridMask;
      if(yd >= halfGrid) pt.Y += gridSize;
    }
    return pt;
  }

  /// <summary>Converts a point from screen coordinate space to virtual coordinate space.</summary>
  static Point ToVirtualPoint(SPoint screenPt)
  {
    double zoomFactor = 2.0 / (zoomLevel*Math.Min(Video.Width, Video.Height));
    return new Point((screenPt.X-Video.Width /2)*zoomFactor+cameraPoint.X,
                     (screenPt.Y-Video.Height/2)*zoomFactor+cameraPoint.Y);
  }

  /// <summary>Converts a point from virtual coordinate space to screen space.</summary>
  static SPoint ToScreenPoint(Point virtualPt)
  {
    // virtual points range from -1 to 1 on both axes.
    // with a zoom level of 1.0 and a camera at (0,0), this corresponds a square area in the middle of the screen.
    // choose a zoom factor that will scale it into the range 0-width_of_square
    double zoomFactor = zoomLevel * 0.5 * Math.Min(Video.Width, Video.Height);
    return new SPoint((int)((virtualPt.X-cameraPoint.X) * zoomFactor)+Video.Width /2,
                      (int)((virtualPt.Y-cameraPoint.Y) * zoomFactor)+Video.Height/2);
  }

  static void UpdateButtonOver()
  {
    int newButtonOver = -1;
    for(int i=0; i<Buttons.Length; i++) // update button highlighting
    {
      if(Buttons[i].Contains(Mouse.Point))
      {
        newButtonOver = i;
        break;
      }
    }
    if(newButtonOver != buttonOver)
    {
      buttonOver = newButtonOver;
      doRepaint  = true;
    }
  }

  /// <summary>Given a screen point, checks if the point is within any vertex and updates the Highlight field
  /// accordingly.
  /// </summary>
  static void UpdateHighlights()
  {
    int newHighlight = -1;
    for(int i=Vertices.Length-1; i >= 0; i--)
    {
      if(Vertices[i].Contains(Mouse.Point))
      {
        newHighlight = i;
        break;
      }
    }

    if(newHighlight != highlighted)
    {
      highlighted = newHighlight;
      doRepaint   = true;
    }
  }

  const int MaxConnections = 3;

  #region Button
  struct Button
  {
    public Button(string letter, string text, int xoffset, int yoffset, ClickHandler handler)
    {
      Letter  = letter;
      Text    = text;
      XOffset = xoffset;
      YOffset = yoffset;
      Handler = handler;
    }

    public const int Spacing = 5, Radius = 15;

    public delegate void ClickHandler();

    // determines whether the button contains the given screen point
    public bool Contains(SPoint screenPt)
    {
      SPoint center = GetScreenPoint();
      int xd = screenPt.X-center.X, yd = screenPt.Y-center.Y;
      return xd*xd+yd*yd <= Radius*Radius;
    }

    public SPoint GetTextPoint()
    {
      SPoint pt = GetScreenPoint();
      int spacing = Math.Sign(XOffset) * (Radius + Spacing);
      if(spacing < 0) spacing -= font.CalculateSize(Text).Width;

      pt.X += spacing;
      pt.Y -= font.Height / 2; // center the font around the Y position of the button
      return pt;
    }

    public SPoint GetScreenPoint()
    {
      return new SPoint(XOffset < 0 ? Video.Width+XOffset : XOffset, YOffset < 0 ? Video.Height+YOffset : YOffset);
    }

    public void Render(int myIndex)
    {
      SPoint centerPt = GetScreenPoint();

      Color buttonColor = Color.DarkGray;
      Primitives.FilledCircle(Video.DisplaySurface, centerPt, Radius, buttonColor);

      Color letterColor = buttonPress == myIndex && buttonOver == myIndex ? Color.FromArgb(64, 64, 64) : Color.White;
      System.Drawing.Size size = font.CalculateSize(Letter);
      font.Color       = letterColor;
      font.BackColor   = buttonColor;
      font.Render(Video.DisplaySurface, Letter, centerPt.X-size.Width/2, centerPt.Y-size.Height/2);

      if(buttonPress == myIndex || buttonOver == myIndex)
      {
        font.Color     = buttonPress == myIndex && buttonOver == myIndex ? letterColor : buttonColor;
        font.BackColor = Color.White;
        font.Render(Video.DisplaySurface, Text, GetTextPoint());
      }
    }

    public readonly string Letter, Text;
    public readonly ClickHandler Handler;
    readonly int XOffset, YOffset;
  }
  #endregion

  #region Connection
  struct Connection
  {
    public Connection(int first, int second)
    {
      First  = first;
      Second = second;
      Failed = false;
    }

    public Connection(BinaryReader br)
    {
      First  = br.ReadInt32();
      Second = br.ReadInt32();
      Failed = br.ReadBool();
    }

    public Line GetLine()
    {
      return new Line(Vertices[First].Position, Vertices[Second].Position);
    }

    public bool Hasvertex(int vertex)
    {
      return First == vertex || Second == vertex;
    }

    public override string ToString()
    {
      return string.Format("{0}-{1}", First, Second);
    }

    public void Write(BinaryWriter bw)
    {
      bw.Write(First);
      bw.Write(Second);
      bw.Write(Failed);
    }

    public int First, Second;
    public bool Failed;
  }
  #endregion

  #region LineComparer
  sealed class LineComparer : IComparer<int>
  {
    public LineComparer(Line[] lines, int baseLine)
    {
      this.lines    = lines;
      this.baseLine = baseLine;
    }

    public int Compare(int a, int b)
    {
      // sort the lines by the points where they intersect baseLine
      Point ia = Math2D.Intersection(ref lines[baseLine], ref lines[a]),
            ib = Math2D.Intersection(ref lines[baseLine], ref lines[b]);
      return ia.X < ib.X ? -1 : ia.X > ib.X ? 1 : ia.Y < ib.Y ? -1 : ia.Y > ib.Y ? 1 : 0;
    }

    readonly Line[] lines;
    readonly int baseLine;
  }
  #endregion

  #region Vertex
  struct Vertex
  {
    public Vertex(BinaryReader br)
    {
      Position = new Point(br.ReadDouble(), br.ReadDouble());
      Moved    = br.ReadBool();
      ConnectedToHighlight = false;
    }
    
    public int Radius
    {
      get { return Moved ? 5 : 8; }
    }

    public bool Contains(SPoint screenPt)
    {
      SPoint center = GetScreenPoint();
      int xd = screenPt.X-center.X, yd = screenPt.Y-center.Y;
      return xd*xd+yd*yd <= Radius*Radius;
    }

    public SPoint GetScreenPoint()
    {
      return Game.ToScreenPoint(Position);
    }

    public void Write(BinaryWriter bw)
    {
      bw.Write(Position.X);
      bw.Write(Position.Y);
      bw.Write(Moved);
    }

    /// <summary>The location of the vertex. At a normal zoom factor, the coordinates range from -1 (top, left) to
    /// 1 (bottom, right).
    /// </summary>
    public Point Position;
    public bool Moved, ConnectedToHighlight;
  }
  #endregion

  static readonly Button[] Buttons = new Button[]
    {
      new Button("L", "Skip to Level",    Button.Spacing+Button.Radius, -Button.Spacing*2 - Button.Radius*3, SkipToLevel),
      new Button("S", "Shuffle Vertices", Button.Spacing+Button.Radius, -Button.Spacing   - Button.Radius,   ShuffleVertices),
      new Button("C", "Check Solution",  -Button.Spacing-Button.Radius, -Button.Spacing*2 - Button.Radius*3, CheckSolution),
      new Button("P", "Pause",           -Button.Spacing-Button.Radius, -Button.Spacing   - Button.Radius,   Pause)
    };

  const MouseButton NotDragging = MouseButton.WheelDown;

  static Vertex[] Vertices;
  static Connection[] Connections;
  static Point[] savedPoints;
  static readonly List<int> selected = new List<int>();
  static TrueTypeFont font;
  static readonly Random rand = new Random();
  static Point cameraPoint, dragPoint, mouseDownPoint;
  static double zoomLevel = 1, timeOffset;
  static int score, level, dragVertex, highlighted, buttonPress, buttonOver, gridSize = MaxGridSize;
  static MouseButton dragButton = NotDragging;
  static bool doRepaint = true, paused;
}

} // namespace Flatarity