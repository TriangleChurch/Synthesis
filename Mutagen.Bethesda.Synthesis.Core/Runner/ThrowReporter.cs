﻿using Mutagen.Bethesda.Synthesis.Core.Patchers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.Core.Runner
{
    public class ThrowReporter : IRunReporter
    {
        public static ThrowReporter Instance = new ThrowReporter();

        private ThrowReporter()
        {
        }

        public void ReportOutputMapping(IPatcher patcher, string str)
        {
        }

        public void ReportOverallProblem(Exception ex)
        {
            throw ex;
        }

        public void ReportPrepProblem(IPatcher patcher, Exception ex)
        {
            throw ex;
        }

        public void ReportRunProblem(IPatcher patcher, Exception ex)
        {
            throw ex;
        }
    }
}
