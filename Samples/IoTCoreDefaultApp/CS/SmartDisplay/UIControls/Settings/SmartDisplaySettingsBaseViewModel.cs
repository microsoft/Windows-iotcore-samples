// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;

namespace SmartDisplay.Controls
{
    public class SmartDisplaySettingsBaseViewModel : SettingsBaseViewModel
    {
        protected SettingsProvider Settings => SettingsProvider as SettingsProvider;

        public SmartDisplaySettingsBaseViewModel() : base(SmartDisplay.AppService.GetForCurrentContext())
        {
        }
    }
}
