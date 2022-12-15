﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLAIF_VisionPlatform.Interface
{
    public interface Observer
    {
        enum Cmd
        {
            UpdateFromJson,
            UpdateToJson,
            PickPoseUpdated,
            UpdateView,
        }

        void Update(Cmd cmd);
    }
}
