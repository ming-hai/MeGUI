﻿// ****************************************************************************
// 
// Copyright (C) 2005-2009  Doom9 & al
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using MeGUI.core.util;

namespace MeGUI
{
    public class FFMSIndexer : CommandlineJobProcessor<FFMSIndexJob>
    {
        public static readonly JobProcessorFactory Factory =
                    new JobProcessorFactory(new ProcessorFactory(init), "FFMSIndexer");

        private static IJobProcessor init(MainForm mf, Job j)
        {
            if (j is FFMSIndexJob) return new FFMSIndexer(mf.Settings.FFMSIndexPath);
            return null;
        }

        private string lastLine;

        public FFMSIndexer(string executableName)
        {
            executable = executableName;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            if (Regex.IsMatch(line, "^Indexing, please wait... [0-9]{1,3}%", RegexOptions.Compiled))
            {
                su.PercentageDoneExact = Int32.Parse(line.Substring(25).Split('%')[0]);
                su.Status = "Creating FFMS index...";
            }
            else if (Regex.IsMatch(line, "^Writing index...", RegexOptions.Compiled))
            {
                //su.PercentageDoneExact = 100;
                su.Status = "Writing FFMS index...";
            }
            else
                base.ProcessLine(line, stream);

            lastLine = line;
        }

        protected override string Commandline
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (job.DemuxMode > 0)
                    sb.Append("-t -1 ");
                sb.Append("-f \"" + job.Input + "\"");
                return sb.ToString();
            }
        }

        protected override void doExitConfig()
        {
            if (job.DemuxMode > 0)
            {
                foreach (AudioTrackInfo oAudioTrack in job.AudioTracks)
                {
                    string strAudioAVSFile;
                    strAudioAVSFile = job.Input + "_track_" + (oAudioTrack.Index + 1) + "_" + oAudioTrack.Language.ToLower() + ".avs";
                    try
                    {
                        StreamWriter oAVSWriter = new StreamWriter(strAudioAVSFile, false, Encoding.Default);
                        String strDLLPath = Path.Combine(Path.GetDirectoryName(MainForm.Instance.Settings.FFMSIndexPath), "ffms2.dll");
#if x86
                        oAVSWriter.WriteLine("LoadPlugin(\"" + strDLLPath + "\")\r\nFFAudioSource(\"" + job.Input + "\", " + (oAudioTrack.Index + 1) + ")");
#endif
#if x64
                        oAVSWriter.WriteLine("LoadCPlugin(\"" + strDLLPath + "\")\r\nFFAudioSource(\"" + job.Input + "\", " + (oAudioTrack.Index + 1) + ")");
#endif
                        oAVSWriter.Close();
                    }
                    catch (IOException ex)
                    {
                        log.LogValue("Error creating audio AVS file", ex);
                    }
                }
            }
            base.doExitConfig();
        }
    }
}
