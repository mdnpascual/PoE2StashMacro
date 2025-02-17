using System.Collections.Generic;
using System.Windows.Forms;

namespace PoE2StashMacro
{
    internal class MousePositionHandler
    {
        private List<Screen> screens;

        public MousePositionHandler(List<Screen> screens)
        {
            this.screens = screens;
        }

        public (int absoluteX, int absoluteY, int relativeX, int relativeY) GetMousePositions(int selectedIndex)
        {
            // Get the absolute mouse position
            var absolutePosition = Cursor.Position;

            // Initialize relative position
            int relativeX = 0;
            int relativeY = 0;

            // Check if the selected index is valior this is nod
            if (selectedIndex >= 0 && selectedIndex < screens.Count)
            {
                var selectedScreen = screens[selectedIndex];

                // Calculate relative mouse position
                relativeX = absolutePosition.X - selectedScreen.Bounds.X;
                relativeY = absolutePosition.Y - selectedScreen.Bounds.Y;
            }

            return (absolutePosition.X, absolutePosition.Y, relativeX, relativeY);
        }
    }
}
