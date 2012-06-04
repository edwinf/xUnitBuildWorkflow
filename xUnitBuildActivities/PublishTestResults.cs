using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using Microsoft.TeamFoundation.Build.Client;
using System.IO;
using System.Xml.Xsl;
using System.Xml;
using System.Diagnostics;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using Microsoft.TeamFoundation.Build.Workflow.Services;

namespace CustomBuildActivities
{
	[BuildActivity(HostEnvironmentOption.Agent)]
	public sealed class PublishTestResults : CodeActivity
	{
		public InArgument<string> ResultFileToPublish { get; set; }

		public InArgument<BuildSettings> BuildSettings { get; set; }

		protected override void Execute(CodeActivityContext context)
		{
			string resultFileToPublish = this.ResultFileToPublish.Get(context);

			string resultTrxFile = Path.Combine(Path.GetDirectoryName(resultFileToPublish), Path.GetFileNameWithoutExtension(resultFileToPublish) + ".trx");

			if (!File.Exists(resultFileToPublish))
			{
				//this could be normal if there were no tests in the file
				context.TrackBuildMessage("Could not find results file: " +  resultFileToPublish  +", skipping publish activity", BuildMessageImportance.Low);
				return;
			}

			this.TransformNUnitToMSTest(context, resultFileToPublish, resultTrxFile);

			var buildDetail = context.GetExtension<IBuildDetail>();
			string collectionUrl = buildDetail.BuildServer.TeamProjectCollection.Uri.ToString();
			string buildNumber = buildDetail.BuildNumber;
			string teamProject = buildDetail.TeamProject;
			string platform = BuildSettings.Get(context).PlatformConfigurations[0].Platform;
			string flavor = BuildSettings.Get(context).PlatformConfigurations[0].Configuration;
			this.PublishMSTestResults(context, resultTrxFile, collectionUrl, buildNumber, teamProject, platform, flavor);
		}

		private void PublishMSTestResults(CodeActivityContext context, string resultTrxFile, string collectionUrl, string buildNumber, string teamProject, string platform, string flavor)
		{
			string argument = string.Format("/publish:\"{0}\" /publishresultsfile:\"{1}\" /publishbuild:\"{2}\" /teamproject:\"{3}\" /platform:\"{4}\" /flavor:\"{5}\"", collectionUrl, resultTrxFile, buildNumber, teamProject, platform, flavor);
			this.RunProcess(context, Environment.ExpandEnvironmentVariables(@"%VS100COMNTOOLS%\..\IDE\MSTest.exe"), null, argument);
		}

		private void TransformNUnitToMSTest(CodeActivityContext context, string nunitResultFile, string mstestResultFile)
		{
			context.TrackBuildMessage("input file: " + nunitResultFile);
			context.TrackBuildMessage("output file: " + mstestResultFile);
			Stream s = this.GetType().Assembly.GetManifestResourceStream("CustomBuildActivities.NUnitToMSTest.xslt");
			if (s == null)
			{
				context.TrackBuildError("Could not load NUnitToMSTest.xslt from embedded resources");
				return;
			}

			using (var reader = new XmlTextReader(s))
			{
				XslCompiledTransform transform = new XslCompiledTransform();
				transform.Load(reader);
				transform.Transform(nunitResultFile, mstestResultFile);
			}
		}

		private int RunProcess(CodeActivityContext context, string fullPath, string workingDirectory, string arguments)
		{
			context.TrackBuildMessage("fullPath: " + fullPath, BuildMessageImportance.Low);
			context.TrackBuildMessage("workingDir: " + workingDirectory, BuildMessageImportance.Low);
			context.TrackBuildMessage("arguments: " + arguments, BuildMessageImportance.Low);
			using (Process proc = new Process())
			{
				proc.StartInfo.FileName = fullPath;

				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.StartInfo.RedirectStandardError = true;
				proc.StartInfo.Arguments = arguments;
				context.TrackBuildMessage("Running " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments, BuildMessageImportance.High);

				if (!string.IsNullOrEmpty(workingDirectory))
				{
					proc.StartInfo.WorkingDirectory = workingDirectory;
				}

				proc.Start();

				string outputStream = proc.StandardOutput.ReadToEnd();
				if (outputStream.Length > 0)
				{
					context.TrackBuildMessage(outputStream, BuildMessageImportance.Normal);
				}

				string errorStream = proc.StandardError.ReadToEnd();
				if (errorStream.Length > 0)
				{
					context.TrackBuildError(errorStream);
				}

				proc.WaitForExit();
				context.TrackBuildMessage("Exit Code: " + proc.ExitCode, BuildMessageImportance.Low);
				return proc.ExitCode;
			}
		}
	}
}
