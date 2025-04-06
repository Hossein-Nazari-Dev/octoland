using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace octoland
{
    public class octolandInfo : GH_AssemblyInfo
    {
        public override string Name => "octoland";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "A set of parametric tools for landscape design";

        public override Guid Id => new Guid("1101f6fc-bac7-471a-9054-158378c7daa1");

        //Return a string identifying you or your company.
        public override string AuthorName => "NexoNest";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "www.NexoNest.com";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}