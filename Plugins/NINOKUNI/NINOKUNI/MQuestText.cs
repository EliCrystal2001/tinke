﻿// ----------------------------------------------------------------------
// <copyright file="MQuestText.cs" company="none">

// Copyright (C) 2012
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by 
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful, 
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details. 
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>. 
//
// </copyright>

// <author>pleoNeX</author>
// <email>benito356@gmail.com</email>
// <date>29/04/2012 13:38:10</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms;
using Ekona;

namespace NINOKUNI
{
    public partial class MQuestText : UserControl
    {
        int id;
        IPluginHost pluginHost;
        string fileName;
        MainQuest mq_old;
        MainQuest mq;
        Encoding enc;

        public MQuestText()
        {
            InitializeComponent();
        }
        public MQuestText(IPluginHost pluginHost, string file, int id)
        {
            InitializeComponent();
            this.id = id;
            this.pluginHost = pluginHost;
            fileName = Path.GetFileNameWithoutExtension(file).Substring(12);
            enc = Encoding.GetEncoding("shift_jis");

            mq = ReadFile(file);
            mq_old = ReadFile(file);
            numBlock.Maximum = mq.num_blocks - 1;
            lblBlockNum.Text = "of " + numBlock.Maximum;
            numBlock_ValueChanged(null, null);
        }


        private MainQuest ReadFile(string file)
        {
            BinaryReader br = new BinaryReader(File.OpenRead(file));
            MainQuest mq = new MainQuest();

            mq.num_blocks = br.ReadUInt16();
            mq.blocks = new MainQuest.Block[mq.num_blocks];

            for (int i = 0; i < mq.num_blocks; i++)
            {
                Console.WriteLine("Reading block " + i.ToString());
                mq.blocks[i].size = br.ReadUInt16();
                mq.blocks[i].id = br.ReadUInt32();

                List<MainQuest.Block.Element> elements = new List<MainQuest.Block.Element>();
                int pos = 4;
                while (pos + 1 != mq.blocks[i].size)
                {
                    Console.WriteLine("\tElement " + (elements.Count + 1).ToString());
                    MainQuest.Block.Element e = new MainQuest.Block.Element();
                    e.size = br.ReadUInt16();
                    e.text = new String(Encoding.GetEncoding(932).GetChars(br.ReadBytes((int)e.size)));
                    e.text = Helper.SJISToLatin(e.text);

                    elements.Add(e);
                    pos += 2 + e.size;
                }
                br.ReadByte();  // 00

                mq.blocks[i].elements = elements.ToArray();
            }

            br.Close();
            return mq;
        }
        private void Write(string fileOut)
        {
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(fileOut));

            bw.Write(mq.num_blocks);

            for (int i = 0; i < mq.blocks.Length; i++)
            {
                Update_Block(ref mq.blocks[i]);
                bw.Write(mq.blocks[i].size);
                bw.Write(mq.blocks[i].id);

                for (int e = 0; e < mq.blocks[i].elements.Length; e++)
                {
                    bw.Write(mq.blocks[i].elements[e].size);
                    bw.Write(enc.GetBytes(Helper.LatinToSJIS(mq.blocks[i].elements[e].text)));
                }
                bw.Write((byte)0x00);
            }

            bw.Flush();
            bw.Close();
        }
        private void Update_Block(ref MainQuest.Block block)
        {
            ushort size = 0;
            for (int i = 0; i < block.elements.Length; i++)
            {
                MainQuest.Block.Element e = block.elements[i];
                e.size = (ushort)enc.GetByteCount(Helper.LatinToSJIS(e.text));
                block.elements[i] = e;

                size += (ushort)(e.size + 2);
            }
            size += 5;  // ID + last 0 byte
            block.size = size;
        }

        private void numBlock_ValueChanged(object sender, EventArgs e)
        {
            int i = (int)numBlock.Value;
            numBlockID.Value = mq.blocks[i].id;

            numElement.Maximum = mq.blocks[i].elements.Length - 1;
            lblNum.Text = "of " + numElement.Maximum;
            numElement.Value = 0;
            numElement_ValueChanged(null, null);
        }
        private void numElement_ValueChanged(object sender, EventArgs e)
        {
            int i = (int)numBlock.Value;
            int j = (int)numElement.Value;

            txtOri.Text = mq_old.blocks[i].elements[j].text.Replace("\n", "\r\n");
            lblSizeOri.Text = "Size: " + txtOri.Text.Length;
            txtTrans.Text = mq.blocks[i].elements[j].text.Replace("\n", "\r\n"); ;
            lblSizeTrans.Text = "";
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog o = new SaveFileDialog();
            o.AddExtension = true;
            o.CheckPathExists = true;
            o.DefaultExt = ".xml";
            o.FileName = fileName + ".xml";
            if (o.ShowDialog() != DialogResult.OK)
                return;

            if (File.Exists(o.FileName))
                File.Delete(o.FileName);

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root = doc.CreateElement("MainQuest");

            for (int i = 0; i < mq.blocks.Length; i++)
            {
                XmlElement block = doc.CreateElement("Block");
                block.SetAttribute("ID", mq.blocks[i].id.ToString("x"));

                for (int j = 0; j < mq.blocks[i].elements.Length; j++)
                {
                    XmlElement el = doc.CreateElement("String");

                    // Format the text
                    string text = mq.blocks[i].elements[j].text;
                    text = text.Replace('<', '【');
                    text = text.Replace('>', '】');
                    text = text.Replace("\n", "\n      ");
                    if (text.Contains("\n"))
                        text = "\n      " + text + "\n    ";

                    el.InnerText = text;
                    block.AppendChild(el);
                }

                root.AppendChild(block);
            }

            doc.AppendChild(root);
            doc.Save(o.FileName);
        }
        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog o = new OpenFileDialog();
            o.AddExtension = true;
            o.CheckFileExists = true;
            o.DefaultExt = ".xml";
            o.FileName = fileName + ".xml";
            if (o.ShowDialog() != DialogResult.OK)
                return;

            XmlDocument doc = new XmlDocument();
            doc.Load(o.FileName);

            XmlNode root = doc.ChildNodes[1];
            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                XmlNode block = root.ChildNodes[i];
                mq.blocks[i].id = Convert.ToUInt32(block.Attributes["ID"].Value, 16);

                for (int j = 0; j < block.ChildNodes.Count; j++)
                {
                    string text = block.ChildNodes[j].InnerText;
                    if (text.Contains("\n"))
                    {
                        text = text.Remove(0, 7);
                        text = text.Remove(text.Length - 5);
                        text = text.Replace("\n      ", "\n");
                    }
                    text = text.Replace('【', '<');
                    text = text.Replace('】', '>');

                    mq.blocks[i].elements[j].text = text;
                }
            }
            numBlock_ValueChanged(null, null);
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            string fileOut = pluginHost.Get_TempFolder() + Path.DirectorySeparatorChar + Path.GetRandomFileName();
            Write(fileOut);
            pluginHost.ChangeFile(id, fileOut);
        }

        private void txtTrans_TextChanged(object sender, EventArgs e)
        {
            int i = (int)numBlock.Value;
            int j = (int)numElement.Value;

            MainQuest.Block.Element el = mq.blocks[i].elements[j];
            el.text = txtTrans.Text.Replace("\r\n", "\n");
            mq.blocks[i].elements[j] = el;
        }
        private void numBlockID_ValueChanged(object sender, EventArgs e)
        {
            int i = (int)numBlock.Value;
            mq.blocks[i].id = (uint)numBlockID.Value;
        }

    }
    public struct MainQuest
    {
        public ushort num_blocks;
        public Block[] blocks;

        public struct Block
        {
            public ushort size;
            public uint id;
            public Element[] elements;

            public struct Element
            {
                public ushort size;
                public string text;
            }
        }
    }

}
