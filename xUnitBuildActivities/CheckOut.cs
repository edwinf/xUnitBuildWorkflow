using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.TeamFoundation.Build.Client;

namespace Rosetta.Microsoft.TFS.CustomBuildActivities
{
	[BuildActivity(HostEnvironmentOption.Agent)]
	public sealed class GenerateGuid : CodeActivity
	{
		
		public InArgument<string> TfsServer { get; set; }

		public InArgument<string> FileNames { get; set; }

		public InArgument<string> WorkspaceName { get; set; }

		// If your activity returns a value, derive from CodeActivity<TResult>
		// and return the value from the Execute method.
		protected override void Execute(CodeActivityContext context)
		{
			 
		}
	}
}
