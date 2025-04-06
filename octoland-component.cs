using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.Geometry.Intersect;


namespace OctoLand
{
    public class ElevationAnalysis : GH_Component
    {
        public ElevationAnalysis()
          : base("surface elevation analysis", "SEA",
            "Analysis a surface which represent terrain as landsapce.",
            "octo land", "morphology")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Reference Plane", "P", "Reference Plane As A paramter for contours", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddSurfaceParameter("Surface", "Srf", "Surface to be analyzed", GH_ParamAccess.item);
            pManager.AddNumberParameter("U-Resolution", "U", "Precision amount of Analysis in U direction", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("V-Resolution", "V", "Precision amount of Analysis", GH_ParamAccess.item, 1);

        }

    
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh of the Surface", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Points of the Surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Relative Elevation", "RE", "Relative Elevation of the Surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Absolute Elevation", "AE", "Absolute Elevation of the Surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Average Elevation", "AvrE", "Average Elevation of the Surface", GH_ParamAccess.item);
            pManager.AddNumberParameter("Standard Deviation", "SD", "Standard Deviation of the Surface Elevation", GH_ParamAccess.item);
            pManager.AddCurveParameter("Ground contour", "GC", "Ground contour of the surface", GH_ParamAccess.list);
            pManager.HideParameter(1);
        }

 
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Surface terrain = null;
            Plane plane = Plane.WorldXY;
            plane.Origin = new Point3d(0, 0, 0);
            double UResolution = 1;
            double VResolution = 1;
            if (!DA.GetData(0, ref plane)) return;
            if (!DA.GetData(1, ref terrain)) return;
            if (!DA.GetData(2, ref UResolution)) return;
            if (!DA.GetData(3, ref VResolution)) return;

            if (UResolution <= 0 || VResolution <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Resolution must be greater than zero.");
                return;
            }

            if (terrain.IsClosed(0)|| terrain.IsClosed (1)) 
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The input surface cannot be closed.");
                return;
            }


            Mesh resultMesh = SurfaceToMesh(terrain, UResolution, VResolution);
            if (!resultMesh.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh comes from Surface is not valid.");
                return;
            }


            MeshVertexList vertices = resultMesh.Vertices;
            List<Point3d> points = vertices.ToPoint3dArray().ToList();

            List<double> relativeElevation = points.Select(point => point.Z).ToList();
            List<double> absoluteElevation = points.Select(point => Math.Abs(point.Z - plane.OriginZ)).ToList();

            Curve[] curves;


            Intersection.BrepPlane(terrain.ToBrep(), plane, 0.01, out curves, out _);




            double AverageElevation = ArithmeticMean(relativeElevation);
            double standardDeviation = CalculateStandardDeviation(relativeElevation, plane.OriginZ);

            DA.SetData(0, resultMesh);
            DA.SetDataList(1, points);
            DA.SetDataList(2, relativeElevation);
            DA.SetDataList(3, absoluteElevation);
            DA.SetData(4, AverageElevation);
            DA.SetData(5, standardDeviation);
            DA.SetDataList(6, curves);


        }

        private static double SimpleSum(List<double> numbers)
        {
            double sum = 0;
            foreach (double number in numbers)
            {
                sum += number;
            }
            return sum;
        }

        private static double ArithmeticMean(List<double> numbers)
        {
            double sum = SimpleSum(numbers);
            return sum / numbers.Count;
        }

        private static double CalculateStandardDeviation(List<double> numbers, double? referenceValue = null)
        {
            double mean = referenceValue ?? ArithmeticMean(numbers);
            double sumOfSquares = numbers.Sum(val => Math.Pow(val - mean, 2));
            double standardDeviation = Math.Sqrt(sumOfSquares / numbers.Count);
            return standardDeviation;
        }

        private Mesh SurfaceToMesh(Surface srf, double UResolution, double VResolution)
        {
            Interval uDomain = srf.Domain(0);
            Interval vDomain = srf.Domain(1);

            double uLength = uDomain.Length;
            double vLength = vDomain.Length;

            int uCount = (int)(uLength / UResolution) + 1;
            int vCount = (int)(vLength / VResolution) + 1;

            Mesh resultMesh = new Mesh();

            List<Point3d> points = new List<Point3d>();


            for (int i = 0; i < uCount; i++)
            {
                for (int j = 0; j < vCount; j++)
                {
                    double u = uDomain.ParameterAt((double)i / (uCount - 1));
                    double v = vDomain.ParameterAt((double)j / (vCount - 1));
                    Point3d point = srf.PointAt(u, v);
                    points.Add(point);
                    resultMesh.Vertices.Add(point);
                }
            }

            for (int i = 0; i < uCount - 1; i++)
            {
                for (int j = 0; j < vCount - 1; j++)
                {
                    int idx0 = i * vCount + j;
                    int idx1 = idx0 + 1;
                    int idx2 = idx0 + vCount;
                    int idx3 = idx2 + 1;

                    resultMesh.Faces.AddFace(idx0, idx1, idx3, idx2);
                }
            }

            resultMesh.Vertices.CullUnused();
            return resultMesh;


        }




        public override GH_Exposure Exposure => GH_Exposure.primary;


        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("2e0eef9d-5154-4b87-ae71-f5a3a560f97b");
    }
}