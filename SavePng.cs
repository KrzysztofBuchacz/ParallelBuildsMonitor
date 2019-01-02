using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParallelBuildsMonitor
{
    public abstract class SavePng
    {
        /// <summary>
        /// Store visual representation of any control from screen to .png file.
        /// </summary>
        /// <remarks>
        /// This method save even invisible part of control.
        /// </remarks>
        /// <param name="target">Any control like Image, Button, Calendar etc.</param>
        /// <param name="background">Providing background will override control transparent background. This is optional, and can be null.</param>
        /// <param name="fileName">Filename with path, where .png will be saved.</param>
        public static void SaveToPng(Visual target, Brush background, string fileName) //Original name was CreateBitmapFromVisual()
        {
            if (target == null || string.IsNullOrEmpty(fileName))
            {
                Debug.Assert(false, "Wrong SaveToPng() method call. Fix caller!");
                return;
            }

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            if (bounds.IsEmpty)
            {
                Debug.Assert(false, "How it happen that save was not dissabled? Disable SaveAsPng button to avoid wrong call.");
                return;
            }

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((Int32)bounds.Width, (Int32)bounds.Height, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                if (background != null)
                    context.DrawRectangle(background, null, new Rect(new Point(), bounds.Size)); // GraphControl has transparent background. This draw background

                VisualBrush visualBrush = new VisualBrush(target);
                context.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));
            }

            renderTarget.Render(visual);
            PngBitmapEncoder bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
            using (Stream stm = File.Create(fileName))
            {
                bitmapEncoder.Save(stm);
            }
        }
    }
}
