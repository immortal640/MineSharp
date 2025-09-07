using System;
using WixSharp;

namespace WixSharp_Setup1 {
	public class Program {
		static void Main() {
			var project = new Project("MyProduct",
							  new Dir(@"%ProgramFiles%\My Company\My Product",
								  new File("Program.cs")));

			project.GUID = new Guid("621ff329-9653-44f5-ba9f-b69f4f0b0547");
			//project.SourceBaseDir = "<input dir path>";
			//project.OutDir = "<output dir path>";

			project.BuildMsi();
		}
	}
}