﻿// ----------------------------------------------------------------------
// <copyright file="Main.cs" company="none">

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
// <date>27/04/2012 23:53:48</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Ekona;

namespace INAZUMA11
{
    public class Main : IGamePlugin
    {
        IPluginHost pluginHost;
        string gameCode;

        public void Initialize(IPluginHost pluginHost, string gameCode)
        {
            this.gameCode = gameCode;
            this.pluginHost = pluginHost;
        }
        public bool IsCompatible()
        {
            // Inazuma Eleven 1
            if (gameCode == "YEES")
                return true;

            if (gameCode == "BEBJ" || gameCode == "BOEJ" || gameCode == "BEEJ" ||
                gameCode == "YEEP" || gameCode == "YEEJ" || gameCode == "BE8J" ||
                gameCode == "BEZJ" || gameCode == "BEBP" || gameCode == "BEEP")
                    return true;

            return false;
        }

        public Format Get_Format(sFile file, byte[] magic)
        {
            string ext = new string(Encoding.ASCII.GetChars(magic));

            // Pack files
            if ((file.name.ToUpper().EndsWith(".PAC_") || file.name.ToUpper().EndsWith(".PAC")) && BitConverter.ToUInt32(magic, 0) < 0x100)
                return Format.Pack;
            else if (file.name.ToUpper().EndsWith(".PKB"))
                return Format.Pack;
            else if (file.name.ToUpper().EndsWith(".PKH"))
                return Format.System;
            else if (file.name.ToUpper().EndsWith(".SPF_") && ext == "SFP\0")
                return Format.Pack;
            else if (file.name.ToUpper().EndsWith(".SPD"))
                return Format.Pack;
            else if (file.name.ToUpper().EndsWith(".SPL"))
                return Format.System;

            // Text files
            switch (gameCode)
            {
                case "YEES":
                    if (file.id >= 0x01B6 && file.id <= 0x01CA) return Format.Text;
                    if (file.id == 0x619) return Format.Text;
                    if (file.id == 0x61A) return Format.Text;
                    if (file.id == 0xC0) return Format.Text;
                    break;

                case "BEBP":
                    if (file.id >= 0x13F && file.id <= 0x161) return Format.Text;
                    if (file.id == 0xD8) return Format.Text;
                    if (file.id == 0x387) return Format.Text;
                    if (file.id == 0x388) return Format.Text;
                    break;
            }

            return Format.Unknown;
        }

        public sFolder Unpack(sFile file)
        {
            if (file.name.ToUpper().EndsWith(".PAC_") || file.name.ToUpper().EndsWith(".PAC"))
                return PAC.Unpack(file);

            if (file.name.ToUpper().EndsWith(".SPF_"))
                return SFP.Unpack(file.path);

            if (file.name.ToUpper().EndsWith(".PKB"))
            {
                sFile pkh = pluginHost.Search_File((short)(file.id + 1));
                if (pkh.name != file.name)
                {
                    Console.WriteLine("Error searching header file");
                    return new sFolder();
                }

                return PKB.Unpack(file, pkh);
            }

            if (file.name.ToUpper().EndsWith(".SPD"))
            {
                sFile spl = pluginHost.Search_File((short)(file.id + 1));
                if (spl.name != file.name)
                {
                    Console.WriteLine("Error searching header file");
                    return new sFolder();
                }

                return SFP.Unpack(file.path, spl.path);
            }

            return new sFolder();
        }
        public string Pack(ref sFolder unpacked, sFile file)
        {
            string fileout = pluginHost.Get_TempFile();

            if (file.name.ToUpper().EndsWith(".PAC_") || file.name.ToUpper().EndsWith(".PAC"))
            {
                Console.WriteLine("Packing to " + fileout);
                PAC.Pack(ref unpacked, fileout);
                return fileout;
            }

            return null;
        }

        public void Read(sFile file)
        {
        }
        public Control Show_Info(sFile file)
        {
            switch (gameCode)
            {
                    // Inazuma11 - Spanish
                case "YEES":
                    if (file.id >= 0x01B6 && file.id <= 0x01CA) return new SubtitlesControl(file.path, pluginHost, file.id);
                    if (file.id == 0x619) return new BlogpostControl(file.path, file.id, pluginHost);
                    if (file.id == 0x61A) return new BlogresControl(file.path, file.id, pluginHost);
                    if (file.id == 0xC0) return new USearchControl(file.path, file.id, pluginHost);
                    break;

                case "BEBP":
                    if (file.id >= 0x13F && file.id <= 0x161) return new SubtitlesControl(file.path, pluginHost, file.id);
                    if (file.id == 0xD8) return new USearchControl(file.path, file.id, pluginHost);
                    if (file.id == 0x387) return new BlogpostControl(file.path, file.id, pluginHost);
                    if (file.id == 0x388) return new BlogresControl(file.path, file.id, pluginHost);
                    break;
            }
            return new Control();
        }

    }
}
