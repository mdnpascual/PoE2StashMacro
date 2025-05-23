﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using Application = System.Windows.Application;

namespace PoE2StashMacro
{
    internal class DisengageReverse
    {
        private InputAutomation inputAutomation;
        private CancellationToken cancellationToken;
        private Screen screen;
        private string resolution { get; set; }
        private Point centerPoint = new Point(1950, 975);
        private Keys disengageKey;

        public DisengageReverse(string resolution, InputAutomation inputAutomation, CancellationToken cancellationToken, Screen screen, Keys disengageKey)
        {
            this.resolution = resolution;
            this.screen = screen;
            this.inputAutomation = inputAutomation;
            this.cancellationToken = cancellationToken;
            this.disengageKey = disengageKey;

            int screenWidth = screen.Bounds.Width;
            int screenHeight = screen.Bounds.Height;

            centerPoint = new Point((int)Math.Round(screenWidth * 0.5078), (int)Math.Round(screenHeight * 0.4514));
        }

        public void Process(Point cursorPos, System.Windows.Controls.Label label)
        {
            Point oppositeCursorPos = GetOppositeCursorPosition(cursorPos, centerPoint);

            inputAutomation.ReverseDisengageAction(oppositeCursorPos, cursorPos, this.disengageKey);

            Application.Current.Dispatcher.BeginInvoke(() => {
                label.Content = $"X: {cursorPos.X} Y: {cursorPos.Y}\nOpposite X: {oppositeCursorPos.X} Y: {oppositeCursorPos.Y}";
            });
        }

        private Point GetOppositeCursorPosition(Point cursorPos, Point centerPoint)
        {
            // Calculate the difference between cursor position and center point
            double deltaX = cursorPos.X - centerPoint.X;
            double deltaY = cursorPos.Y - centerPoint.Y;

            // Calculate the angle (in degrees) using atan2
            double angleInDegrees = Math.Atan2(deltaY, deltaX) * (180 / Math.PI);

            // Calculate the radius (distance from center to cursor)
            double radius = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // Calculate the opposite angle
            double oppositeAngleInDegrees = angleInDegrees + 180;

            // Convert back to radians for trigonometric functions
            double oppositeAngleInRadians = oppositeAngleInDegrees * (Math.PI / 180);

            // Calculate the new cursor position
            int newX = (int)(centerPoint.X + radius * Math.Cos(oppositeAngleInRadians));
            int newY = (int)(centerPoint.Y + radius * Math.Sin(oppositeAngleInRadians));

            // Get the screen bounds
            var screenBounds = Screen.FromPoint(centerPoint).Bounds;

            // Check if the new position is out of bounds and adjust if necessary
            if (newX < screenBounds.Left || newX > screenBounds.Right || newY < screenBounds.Top || newY > screenBounds.Bottom)
            {
                // Calculate the maximum radius that keeps the position inside the screen bounds
                double maxRadius = Math.Min(
                    Math.Min(screenBounds.Right - centerPoint.X, centerPoint.X - screenBounds.Left),
                    Math.Min(screenBounds.Bottom - centerPoint.Y, centerPoint.Y - screenBounds.Top)
                );

                // Calculate the new position with the adjusted radius
                newX = (int)(centerPoint.X + maxRadius * Math.Cos(oppositeAngleInRadians));
                newY = (int)(centerPoint.Y + maxRadius * Math.Sin(oppositeAngleInRadians));
            }

            return new Point(newX, newY);
        }
    }
}
