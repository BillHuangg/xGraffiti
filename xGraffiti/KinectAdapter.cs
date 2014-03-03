// -----------------------------------------------------------------------
// <copyright file="KinectAdapter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace xGraffiti
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;

    using Microsoft.Kinect.Toolkit.Interaction;

    internal class KinectAdapter : IInteractionClient
    {
        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
        {
            InteractionInfo interactionInfo = new InteractionInfo
            {
                IsPressTarget = false,
                IsGripTarget = false,
                PressAttractionPointX = 0.5,
                PressAttractionPointY = 0.5,
                PressTargetControlId = 0
            };
            return interactionInfo;
        }

    }

    
}
