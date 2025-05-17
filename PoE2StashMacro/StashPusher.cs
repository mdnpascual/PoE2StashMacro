using System;
using System.Windows;
using System.Collections.Generic;
using System.Drawing;

using Point = System.Drawing.Point;
using Application = System.Windows.Application;
using System.Threading;
using System.Windows.Forms;

namespace PoE2StashMacro
{
    internal class StashPusher
    {
        public float boxWidth { get; private set; }
        public float boxHeight { get; private set; }
        public float centerXOffset { get; private set; }
        public float centerYOffset { get; private set; }
        public Point startingPos { get; private set; }
        public Rectangle stashBounds { get; private set; }

        private InputAutomation inputAutomation;
        private CancellationToken cancellationToken;
        private Screen screen;
        private int yCount { get; set; }
        private int xCount { get; set; }
        private int totalBoxes { get; set; }
        private string resolution { get; set; }
        private bool isQuad { get; set; }
        private bool isMapTab { get; set; }
        private int indexToUse { get; set; }
        private int redThreshold = 60;

        // [Normal, Quad tab, Map tab]
        private static readonly Dictionary<string, float[]> widthMapping = new Dictionary<string, float[]>
        {
            { "3840x2160", new float[] { 105, 52.5f, 97 } },
            { "1920x1080", new float[] { 10, 5 } }
        };

        private static readonly Dictionary<string, float[]> heightMapping = new Dictionary<string, float[]>
        {
            { "3840x2160", new float[] { 105, 52.5f, 98 } },
            { "1920x1080", new float[] { 10, 5 } }
        };

        private static readonly Dictionary<string, float[]> xOffsetColorCheckMapping = new Dictionary<string, float[]>
        {
            { "3840x2160", new float[] { 17, -8, 17 } },
            { "1920x1080", new float[] { 10, 5 } }
        };

        private static readonly Dictionary<string, float[]> yOffsetColorCheckMapping = new Dictionary<string, float[]>
        {
            { "3840x2160", new float[] { 25, -1, 24 } },
            { "1920x1080", new float[] { 10, 5 } }
        };

        private static readonly Dictionary<string, Point[]> startPosMapping = new Dictionary<string, Point[]>
        {
            { "3840x2160", new Point[] { new Point(35, 241), new Point(35, 241), new Point(84, 664) } },
            { "1920x1080", new Point[] { new Point(0, 0), new Point(0, 0) } }
        };

        public StashPusher(string resolution, bool isQuad, bool isMapTab, InputAutomation inputAutomation, CancellationToken cancellationToken, Screen screen)
        {
            this.screen = screen;
            this.inputAutomation = inputAutomation;
            this.cancellationToken = cancellationToken;
            SetValues(resolution, isQuad, isMapTab);
        }

        private void SetValues(string resolution, bool isQuad, bool isMapTab)
        {
            this.resolution = resolution;
            this.isQuad = isQuad;
            this.isMapTab = isMapTab;
            this.indexToUse = isMapTab 
                ? 2 
                : isQuad 
                    ? 1 
                    : 0;

            if (widthMapping.TryGetValue(resolution, out float[] widthValues))
            {
                boxWidth = widthValues[this.indexToUse];
            }
            else
            {
                boxWidth = 0;
            }

            if (heightMapping.TryGetValue(resolution, out float[] heightValues))
            {
                boxHeight = heightValues[this.indexToUse];
            }
            else
            {
                boxHeight = 0;
            }

            if (xOffsetColorCheckMapping.TryGetValue(resolution, out float[] xOffsetValues))
            {
                centerXOffset = xOffsetValues[this.indexToUse];
            }
            else
            {
                centerXOffset = 0;
            }

            if (yOffsetColorCheckMapping.TryGetValue(resolution, out float[] yOffsetValues))
            {
                centerYOffset = yOffsetValues[this.indexToUse];
            }
            else
            {
                centerYOffset = 0;
            }

            if (startPosMapping.TryGetValue(resolution, out Point[] startingPosValues))
            {
                startingPos = startingPosValues[this.indexToUse];
            }
            else
            {
                startingPos = new Point(0, 0);
            }

            xCount = (isQuad ? 2 : 1) * 12;
            yCount = isMapTab 
                ? 8 
                : (isQuad ? 2 : 1) * 12;
            totalBoxes = xCount * yCount;

            stashBounds = new Rectangle(startingPos.X, startingPos.Y, (int)Math.Ceiling(boxWidth * xCount), (int)Math.Ceiling(boxHeight * yCount));
        }

        public void Process(Point cursorPos, System.Windows.Controls.Label label)
        {
            int boxIndex = GetBoxIndexFromCursor(cursorPos, label);
            if (boxIndex != -1)
            {
                bool shouldBreak = false;
                while (boxIndex < totalBoxes)
                {
                    if (cancellationToken.IsCancellationRequested || shouldBreak)
                    {
                        break;
                    }
                    inputAutomation.Sleep(100);
                    shouldBreak = ClickBoxAtIndex(boxIndex, label);
                    boxIndex++;
                }
                if (shouldBreak) // Final click if it detected
                {
                    inputAutomation.Sleep(100);
                    shouldBreak = ClickBoxAtIndex(boxIndex, label);
                }
            }
        }

        private bool ClickBoxAtIndex(int boxIndex, System.Windows.Controls.Label label)
        {
            Point position = ComputeBoxPosition(boxIndex);

            // Move the mouse to the computed center point
            inputAutomation.ClickAtPos(position);

            MoveMouseAwayCheck(boxIndex);
            Point nextPos = ComputeBoxPosition(boxIndex + 1);

            int nextXPos = (int)Math.Floor(nextPos.X + centerXOffset);
            int nextYPos = (int)Math.Floor(nextPos.Y + centerYOffset);
            System.Drawing.Color pixelColor = GetPixelColor(nextXPos, nextYPos);
            string colorValue = $"Color at ({nextXPos}, {nextYPos}): R={pixelColor.R}, G={pixelColor.G}, B={pixelColor.B}";

            // Update the label content with the color value
            Application.Current.Dispatcher.Invoke(() => {
                label.Content = colorValue;
            });
            return pixelColor.R < redThreshold;
        }

        private void MoveMouseAwayCheck(int boxIndex)
        {
            if (this.isMapTab && ((boxIndex + 1) % this.yCount) == 0)
            {
                inputAutomation.MouseMove(new Point(0,0), 35);
            }
        }

        private Point ComputeBoxPosition(int boxIndex)
        {
            // Calculate the row and column from the boxIndex
            int row = boxIndex % yCount;
            int column = boxIndex / yCount;

            // Compute the center of the corresponding box
            float centerX = stashBounds.X + (column * boxWidth) + (boxWidth / 2);
            float centerY = stashBounds.Y + (row * boxHeight) + (boxHeight / 2);

            centerX += screen.Bounds.X;
            centerY += screen.Bounds.Y;

            return new Point((int)Math.Ceiling(centerX), (int)Math.Ceiling(centerY));
        }

        private int GetBoxIndexFromCursor(Point cursorPos, System.Windows.Controls.Label label) {
            if (stashBounds.Contains(cursorPos))
            {
                // Calculate the relative position of the cursor within the stash bounds
                int relativeX = cursorPos.X - stashBounds.X;
                int relativeY = cursorPos.Y - stashBounds.Y;

                // Calculate the box index
                int column = (int)Math.Floor(relativeX / boxWidth);
                int row = (int)Math.Floor(relativeY / boxHeight);

                // Calculate the box count vertically first
                int boxIndex = (column * yCount) + row;

                if (boxIndex >= 0 && boxIndex < totalBoxes)
                {
                    UpdateLabel(label, $"Cursor is in box index: {boxIndex}");
                    return boxIndex;
                }
                else
                {
                    UpdateLabel(label, "Cursor is outside the valid box range");
                    return -1;
                }
            }
            else
            {
                UpdateLabel(label, "Cursor is outside the stash bounds.");
            }

            return -1;
        }

        private System.Drawing.Color GetPixelColor(int x, int y)
        {
            using (Bitmap bitmap = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, bitmap.Size);
                }
                return bitmap.GetPixel(0, 0);
            }
        }

        private void UpdateLabel(System.Windows.Controls.Label label, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                label.Content = label.Content +
                $"\n{message}";
            });
        }
    }
}
