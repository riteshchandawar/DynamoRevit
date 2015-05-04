﻿using System;
using System.IO;
using System.Linq;

using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;

using Dynamo.Nodes;

using NUnit.Framework;

using RevitServices.Persistence;

using RevitTestServices;

using RTF.Framework;

namespace RevitSystemTests
{
    [TestFixture]
    class CurveTests : RevitSystemTestBase
    {
        [Test]
        [TestModel(@".\empty.rfa")]
        public void CurveByPoints()
        {
            var model = ViewModel.Model;

            string samplePath = Path.Combine(workingDirectory, @".\Curve\CurveByPoints.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);

            RunCurrentModel();

            //cerate some points and wire them
            //to the selections
            ReferencePoint p1, p2, p3, p4;

            using (var trans = new Transaction(DocumentManager.Instance.CurrentUIDocument.Document))
            {
                trans.Start("Create reference points for testing.");

                p1 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewReferencePoint(new XYZ(1, 5, 12));
                p2 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewReferencePoint(new XYZ(5, 1, 12));
                p3 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewReferencePoint(new XYZ(12, 1, 5));
                p4 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewReferencePoint(new XYZ(5, 12, 1));

                trans.Commit();
            }

            var ptSelectNodes = ViewModel.Model.CurrentWorkspace.Nodes.Where(x => x is DSModelElementSelection);
            if (!ptSelectNodes.Any())
                Assert.Fail("Could not find point selection nodes in dynamo graph.");

            ((DSModelElementSelection)ptSelectNodes.ElementAt(0)).UpdateSelection(new []{p1});;
            ((DSModelElementSelection)ptSelectNodes.ElementAt(1)).UpdateSelection(new []{p2});
            ((DSModelElementSelection)ptSelectNodes.ElementAt(2)).UpdateSelection(new []{p3});
            ((DSModelElementSelection)ptSelectNodes.ElementAt(3)).UpdateSelection(new []{p4});

            RunCurrentModel();

            FilteredElementCollector fec = new FilteredElementCollector(DocumentManager.Instance.CurrentUIDocument.Document);
            fec.OfClass(typeof(CurveElement));

            Assert.AreEqual(fec.ToElements().Count(), 1);

            CurveByPoints mc = (CurveByPoints)fec.ToElements().ElementAt(0);
            Assert.IsTrue(mc.IsReferenceLine);

            //now flip the switch for creating a reference curve
            var boolNode = ViewModel.Model.CurrentWorkspace.Nodes.Where(x => x is DSCoreNodesUI.BoolSelector).First();

            ((DSCoreNodesUI.BasicInteractive<bool>)boolNode).Value = false;

            RunCurrentModel();

            Assert.AreEqual(fec.ToElements().Count(), 1);

            mc = (CurveByPoints)fec.ToElements().ElementAt(0);
            Assert.IsFalse(mc.IsReferenceLine);
        }

        [Test]
        [TestModel(@".\empty.rfa")]
        public void CurveLoop()
        {
            string samplePath = Path.Combine(workingDirectory, @".\Curve\CurveLoop.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);

            RunCurrentModel();

            var thickenCurveNodeID = "8f42e859-9f88-4b4f-b1d8-d3a2841b8d14";
            AssertPreviewCount(thickenCurveNodeID, 4);

            // get all AdaptiveComponent.
            for (int i = 0; i <= 3; i++)
            {
                var thickenCurve = GetPreviewValueAtIndex(thickenCurveNodeID, i) as PolyCurve;
                Assert.IsNotNull(thickenCurve);
            }

            var polyCurve =
                GetPreviewValue("f64fee6e-1b62-4503-b307-ab5b3d516438") as PolyCurve;
            Assert.NotNull(polyCurve);

        }

        [Test]
        [TestModel(@".\empty.rfa")]
        public void CurvebyPointsArc()
        {
            string samplePath = Path.Combine(workingDirectory, @".\Curve\CurvebyPointsArc.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);

            RunCurrentModel();

            FilteredElementCollector fec = new FilteredElementCollector(DocumentManager.Instance.CurrentUIDocument.Document);
            fec.OfClass(typeof(CurveElement));

            Assert.AreEqual(fec.ToElements().Count(), 1);
        }

        [Test]
        [TestModel(@".\empty.rfa")]
        public void OffsetCurve()
        {
            string samplePath = Path.Combine(workingDirectory, @".\Curve\OffsetCurve.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);
            RunCurrentModel();
        }

        [Test]
        [TestModel(@".\empty.rfa")]
        public void ThickenCurve()
        {
            string samplePath = Path.Combine(workingDirectory, @".\Curve\ThickenCurve.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);

            RunCurrentModel();

        }

        [Test]
        [TestModel(@".\empty.rfa")]
        public void CurveByPointsByLineNode()
        {
            //this sample creates a geometric line
            //then creates a curve by points from that line

            var model = ViewModel.Model;

            string samplePath = Path.Combine(workingDirectory, @".\Curve\CurveByPointsByLine.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);

            RunCurrentModel();

            FilteredElementCollector fec = new FilteredElementCollector(DocumentManager.Instance.CurrentUIDocument.Document);
            fec.OfClass(typeof(CurveElement));

            Assert.AreEqual(1, fec.ToElements().Count());

            //now change one of the number inputs and rerun
            //verify that there are still only two reference points in
            //the model
            var node = ViewModel.Model.CurrentWorkspace.Nodes.OfType<DoubleInput>().First();
            node.Value = "12.0";

            RunCurrentModel();

            fec = null;
            fec = new FilteredElementCollector(DocumentManager.Instance.CurrentUIDocument.Document);
            fec.OfClass(typeof(CurveElement));
            Assert.AreEqual(1, fec.ToElements().Count);
        }

        /*
        [Test]
        public void ClosedCurve()
        {
            var model = ViewModel.Model;

            string samplePath = Path.Combine(workingDirectory, @".\Curve\ClosedCurve.dyn");
            string testPath = Path.GetFullPath(samplePath);

            model.Open(testPath);
           
            RunCurrentModel();
            

            var extrudeNode = ViewModel.Model.CurrentWorkspace.Nodes.First(x => x is CreateExtrusionGeometry);

            var result = (Solid)VisualizationManager.GetDrawablesFromNode(extrudeNode).Values.First();
            double volumeMin = 3850;
            double volumeMax = 4050;
            double actualVolume = result.Volume;
            Assert.Greater(actualVolume, volumeMin);
            Assert.Less(actualVolume, volumeMax);
        }

         * */

        [Test, Category("Failure")]
        [TestModel(@".\empty.rfa")]
        public void CurvebyPointsEllipse()
        {
            string samplePath = Path.Combine(workingDirectory, @".\Curve\CurvebyPointsEllipse.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);

            RunCurrentModel();

            FilteredElementCollector fec = new FilteredElementCollector(DocumentManager.Instance.CurrentUIDocument.Document);
            fec.OfClass(typeof(CurveElement));

            Assert.AreEqual(fec.ToElements().Count(), 1);

            CurveByPoints mc = (CurveByPoints)fec.ToElements().ElementAt(0);
        }

        [Test]
        [TestModel(@".\Curve\GetCurveDomain.rfa")]
        public void GetCurveDomain()
        {
            string samplePath = Path.Combine(workingDirectory, @".\Curve\GetCurveDomain.dyn");
            string testPath = Path.GetFullPath(samplePath);

            ViewModel.OpenCommand.Execute(testPath);

            RunCurrentModel();
        }
    }
}
