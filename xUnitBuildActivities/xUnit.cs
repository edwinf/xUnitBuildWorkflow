using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Build.Client;

namespace CustomBuildActivities
{
	[BuildActivity(HostEnvironmentOption.Agent)]
	public partial class xUnitRunner
	{
	}
}
