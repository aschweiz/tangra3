﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tangra.Model.VideoOperations;

namespace Tangra.View
{
    public class ZoomedImageView
    {
        private PictureBox m_ZoomedImage;
        private Form m_OwnerForm;

        public ZoomedImageView(PictureBox zoomedImage, Form ownerForm)
        {
            m_ZoomedImage = zoomedImage;
            m_OwnerForm = ownerForm;
        }

        public void DrawCustomZoomImage(IVideoOperation videoOperation)
        {
            using (Graphics g = Graphics.FromImage(m_ZoomedImage.Image))
            {
                videoOperation.DrawCustomZoomImage(g, m_ZoomedImage.Image.Width, m_ZoomedImage.Image.Height);
                g.Save();
                m_ZoomedImage.Invalidate();
            }            
        }

        public void UpdateImage(Bitmap zoomedBitmap)
        {
            m_ZoomedImage.Image = zoomedBitmap;
            m_ZoomedImage.Refresh();            
        }

        public void ClearZoomedImage()
        {
            using (Graphics gz = Graphics.FromImage(m_ZoomedImage.Image))
            {
                gz.Clear(SystemColors.ControlDark);
            }
            m_ZoomedImage.Refresh();
        }
    }
}
