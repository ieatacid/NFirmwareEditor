﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NFirmware;
using NFirmwareEditor.Core;
using NFirmwareEditor.Managers;
using NFirmwareEditor.Models;
using NFirmwareEditor.UI;

namespace NFirmwareEditor.Windows
{
	internal partial class PreviewResourcePackWindow : EditorDialogWindow
	{
		private readonly Firmware m_firmware;

		private static readonly IDictionary<int, ImageImportMode> s_importModeMap = new Dictionary<int, ImageImportMode>
		{
			{ 0, ImageImportMode.Block1And2 },
			{ 1, ImageImportMode.Block1 },
			{ 2, ImageImportMode.Block2 },
		};

		private readonly IDictionary<int, bool[,]> m_originalImportedImages = new Dictionary<int, bool[,]>();
		private readonly IDictionary<int, bool[,]> m_croppedImportedImages = new Dictionary<int, bool[,]>();

		public PreviewResourcePackWindow()
		{
			InitializeComponent();
			Icon = Paths.ApplicationIcon;
			ResizeCheckBox.CheckedChanged += ResizeCheckBox_CheckedChanged;
		}

		public PreviewResourcePackWindow
		(
			Firmware firmware, 
			IList<int> originalImageIndices, 
			IList<bool[,]> importedImages, 
			bool resourceImport = false, 
			BlockType? defaultImportBlock = null
		) : this()
		{
			if (originalImageIndices.Count != importedImages.Count)
			{
				throw new InvalidOperationException("Source and imported images count does not match.");
			}
			m_firmware = firmware;

			if (m_firmware.Block2Images.Any())
			{
				ImportModeComboBox.Items.Add("Block 1 & 2");
				ImportModeComboBox.Items.Add("Block 1");
				ImportModeComboBox.Items.Add("Block 2");

				if (defaultImportBlock.HasValue)
				{
					ImportModeComboBox.SelectedIndex = defaultImportBlock == BlockType.Block1 ? 1 : 2;
				}
				else
				{
					ImportModeComboBox.SelectedIndex = 0;
				}
			}
			else
			{
				ImportModeComboBox.Items.Add("Block 1");
				ImportModeComboBox.SelectedIndex = 0;
			}

			OptionsGroupBox.Enabled = !resourceImport;
			LeftLayoutPanel.SuspendLayout();
			RightLayoutPanel.SuspendLayout();
			for (var i = 0; i < originalImageIndices.Count; i++)
			{
				var originalImageIndex = originalImageIndices[i];
				var originalImage = GetImageByIndex(originalImageIndex);
				var importedImage = importedImages[i];
				var croppedImportedImage = FirmwareImageProcessor.PasteImage(originalImage, importedImage);

				m_originalImportedImages[i] = importedImage;
				m_croppedImportedImages[i] = croppedImportedImage;

				LeftLayoutPanel.Controls.Add(CreateGrid(originalImage));
				RightLayoutPanel.Controls.Add(CreateGrid(croppedImportedImage));
			}
			LeftLayoutPanel.ResumeLayout();
			RightLayoutPanel.ResumeLayout();
		}

		private bool[,] GetImageByIndex(int index)
		{
			var mode = GetImportMode();
			var block = mode == ImageImportMode.Block2 ? m_firmware.Block2Images : m_firmware.Block1Images;
			var imageMetadata = block.First(x => x.Index == index);

			return m_firmware.ReadImage(imageMetadata);
		}

		public ImageImportMode GetImportMode()
		{
			return s_importModeMap[ImportModeComboBox.SelectedIndex];
		}

		public bool AllowResizeOriginalImages
		{
			get { return ResizeCheckBox.Checked; }
		}

		private void ResizeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			var resizeOriginalImages = ResizeCheckBox.Checked;
			var pixelGrids = RightLayoutPanel.Controls.OfType<PixelGrid>().ToList();

			for (var i = 0; i < pixelGrids.Count; i++)
			{
				pixelGrids[i].Data = resizeOriginalImages ? m_originalImportedImages[i] : m_croppedImportedImages[i];
			}
		}

		private PixelGrid CreateGrid(bool[,] imageData)
		{
			var imageSize = imageData.GetSize();
			var blockSize = imageSize.Height > 64 ? 1 : 2;
			return new PixelGrid
			{
				Width = 132,
				Height = 132,
				Margin = new Padding(1, 1, 1, 0),
				BlockSize = blockSize,
				ShowGrid = false,
				Data = imageData,
				BackColor = Color.Black,
				BlockInnerBorderPen = Pens.Transparent,
				BlockOuterBorderPen = Pens.Transparent,
				ActiveBlockBrush = Brushes.White,
				InactiveBlockBrush = Brushes.Black
			};
		}
	}
}
