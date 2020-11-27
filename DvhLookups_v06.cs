////////////////////////////////////////////////////////////////////////////////
// This is Matthews updated version of the original DvhLookups.cs plugin
// 
// You can find it here: https://github.com/mtparagon5/ESAPI-Projects/tree/d497d7818013296e5fdffbe6cdafcb601e953ac2/Plugins
//
// rest of the lines are original
////////////////////////////////////////////////////////////////////////////////
//  I've added various tools that will allow the user to calculate
//  overlap, shortest distance, as well as ratio calculations e.g., CI and R50
//  There is also a class that will calculate the range of acceptable R50 values 
//  based on RTOG 0915
//  
// Applies to:  ESAPI v11 and later
// NOTE: I'm currently using v13.6 so I apologize if certain things
//       I've added don't work properly in a later version.
//
// Copyright (c) 2015 Varian Medical Systems, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in 
//  all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
//using System.Numerics;

namespace VMS.TPS
{
    #region Class Definitions

    public static class DvhExtensions
    {
        public static DoseValue GetDoseAtVolume(this PlanningItem pitem, Structure structure, double volume, VolumePresentation volumePresentation, DoseValuePresentation requestedDosePresentation)
        {
            if (pitem is PlanSetup)
            {
                return ((PlanSetup)pitem).GetDoseAtVolume(structure, volume, volumePresentation, requestedDosePresentation);
            }
            else
            {
                if (requestedDosePresentation != DoseValuePresentation.Absolute)
                    throw new ApplicationException("Only absolute dose supported for Plan Sums");
                DVHData dvh = pitem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, volumePresentation, 0.001);
                return DvhExtensions.DoseAtVolume(dvh, volume);
            }
        }
        public static double GetVolumeAtDose(this PlanningItem pitem, Structure structure, DoseValue dose, VolumePresentation requestedVolumePresentation)
        {
            if (pitem is PlanSetup)
            {
                return ((PlanSetup)pitem).GetVolumeAtDose(structure, dose, requestedVolumePresentation);
            }
            else
            {
                DVHData dvh = pitem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, requestedVolumePresentation, 0.001);
                return DvhExtensions.VolumeAtDose(dvh, dose.Dose);
            }
        }

        public static DoseValue DoseAtVolume(DVHData dvhData, double volume)
        {
            if (dvhData == null || dvhData.CurveData.Count() == 0)
                return DoseValue.UndefinedDose();
            double absVolume = dvhData.CurveData[0].VolumeUnit == "%" ? volume * dvhData.Volume * 0.01 : volume;
            if (volume < 0.0 /*|| absVolume > dvhData.Volume*/)
                return DoseValue.UndefinedDose();

            DVHPoint[] hist = dvhData.CurveData;
            for (int i = 0; i < hist.Length; i++)
            {
                if (hist[i].Volume < volume)
                    return hist[i].DoseValue;
            }
            return DoseValue.UndefinedDose();
        }

        public static double VolumeAtDose(DVHData dvhData, double dose)
        {
            if (dvhData == null)
                return Double.NaN;

            DVHPoint[] hist = dvhData.CurveData;
            int index = (int)(hist.Length * dose / dvhData.MaxDose.Dose);
            if (index < 0 || index > hist.Length)
                return 0.0;//Double.NaN;
            else
                return hist[index].Volume;
        }

    }
    // NOTE: This is used to calculate the acceptable R50 Range for a Lung SBRT Plan based on PTV Volume -- From RTOG 0915
    public class R50Constraint
    {
        public static void LimitsFromVolume(double volume, out double limit1, out double limit2, out double limit3, out double limit4)
        {
            // larger tah last in the table
            limit1 = 5.9;
            limit2 = 7.5;
            limit3 = 50;
            limit4 = 57;

            if ((volume >= 1.8) && (volume < 3.8))
            {
                limit1 = 5.9 + (volume - 1.8) * (5.5 - 5.9) / (3.8 - 1.8);
                limit2 = 7.5 + (volume - 1.8) * (6.5 - 7.5) / (3.8 - 1.8);
                limit3 = 50 + (volume - 1.8) * (50 - 50) / (3.8 - 1.8);
                limit4 = 57 + (volume - 1.8) * (57 - 57) / (3.8 - 1.8);
            }

            if ((volume >= 3.8) && (volume < 7.4))
            {
                limit1 = 5.5 + (volume - 3.8) * (5.1 - 5.5) / (7.4 - 3.8);
                limit2 = 6.5 + (volume - 3.8) * (6.0 - 6.5) / (7.4 - 3.8);
                limit3 = 50 + (volume - 3.8) * (50 - 50) / (7.4 - 3.8);
                limit4 = 57 + (volume - 3.8) * (58 - 57) / (7.4 - 3.8);
            }

            if ((volume >= 7.4) && (volume < 13.2))
            {
                limit1 = 5.1 + (volume - 7.4) * (4.7 - 5.1) / (13.2 - 7.4);
                limit2 = 6.0 + (volume - 7.4) * (5.8 - 6.0) / (13.2 - 7.4);
                limit3 = 50 + (volume - 7.4) * (54 - 50) / (13.2 - 7.4);
                limit4 = 58 + (volume - 7.4) * (58 - 58) / (13.2 - 7.4); ;
            }

            if ((volume > 13.2) && (volume < 22.0))
            {
                limit1 = 4.7 + (volume - 13.2) * (4.5 - 4.7) / (22.0 - 13.2);
                limit2 = 5.8 + (volume - 13.2) * (5.5 - 5.8) / (22.0 - 13.2);
                limit3 = 50 + (volume - 13.2) * (54 - 50) / (22.0 - 13.2);
                limit4 = 58 + (volume - 13.2) * (63 - 58) / (22.0 - 13.2);
            }

            if ((volume > 22.0) && (volume < 34.0))
            {
                limit1 = 4.5 + (volume - 22.0) * (4.3 - 4.5) / (34.0 - 22.0);
                limit2 = 5.5 + (volume - 22.0) * (5.3 - 5.5) / (34.0 - 22.0);
                limit3 = 54 + (volume - 22.0) * (58 - 54) / (34.0 - 22.0);
                limit4 = 63 + (volume - 22.0) * (68 - 63) / (34.0 - 22.0);
            }

            if ((volume > 34.0) && (volume < 50.0))
            {
                limit1 = 4.3 + (volume - 34.0) * (4.0 - 4.3) / (50.0 - 34.0);
                limit2 = 5.3 + (volume - 34.0) * (5.0 - 5.3) / (50.0 - 34.0);
                limit3 = 58 + (volume - 34.0) * (62 - 58) / (50.0 - 34.0);
                limit4 = 68 + (volume - 34.0) * (77 - 68) / (50.0 - 34.0);
            }

            if ((volume > 50.0) && (volume < 70.0))
            {
                limit1 = 4.0 + (volume - 50.0) * (3.5 - 4.0) / (70.0 - 50.0);
                limit2 = 5.0 + (volume - 50.0) * (4.8 - 5.0) / (70.0 - 50.0);
                limit3 = 62 + (volume - 50.0) * (66 - 62) / (70.0 - 50.0);
                limit4 = 77 + (volume - 50.0) * (86 - 77) / (70.0 - 50.0);
            }

            if ((volume > 70.0) && (volume < 95.0))
            {
                limit1 = 3.5 + (volume - 70.0) * (3.3 - 3.5) / (95.0 - 70.0);
                limit2 = 4.8 + (volume - 70.0) * (4.4 - 4.8) / (95.0 - 70.0);
                limit3 = 66 + (volume - 70.0) * (70 - 66) / (95.0 - 70.0);
                limit4 = 86 + (volume - 70.0) * (89 - 86) / (95.0 - 70.0);
            }

            if ((volume > 95.0) && (volume < 126.0))
            {
                limit1 = 3.3 + (volume - 95.0) * (3.1 - 3.3) / (126.0 - 95.0);
                limit2 = 4.4 + (volume - 95.0) * (4.0 - 4.4) / (126.0 - 95.0);
                limit3 = 70 + (volume - 95.0) * (73 - 70) / (126.0 - 95.0);
                limit4 = 89 + (volume - 95.0) * (91 - 89) / (126.0 - 95.0);
            }

            if ((volume > 126.0) && (volume < 163.0))
            {
                limit1 = 3.1 + (volume - 126.0) * (2.9 - 3.1) / (163.0 - 126.0);
                limit2 = 4.0 + (volume - 126.0) * (3.7 - 4.0) / (163.0 - 126.0);
                limit3 = 73 + (volume - 126.0) * (77 - 73) / (163.0 - 126.0);
                limit4 = 91 + (volume - 126.0) * (94 - 91) / (163.0 - 126.0);
            }

            if ((volume > 163.0))
            {
                limit1 = 2.9;
                limit2 = 3.7;
                limit3 = 77;
                limit4 = 94;
            }
        }
    }
    // This class can be used to calculate:
    //      volume overlap of two structures, percent overlap, shortest distance, 
    //      average distance inside given radius, and average distance outside a given radius
    // TODO: need to find meaningful representation of data that can be calculated
    public class CalculateOverlap
    {
        public static double VolumeOverlap(Structure structure1, Structure structure2)
        {
            // initialize items needed for calculating distance
            VVector p = new VVector();
            double volumeIntersection = 0;
            int intersectionCount = 0;

            Rect3D structure1Bounds = structure1.MeshGeometry.Bounds;
            Rect3D structure2Bounds = structure2.MeshGeometry.Bounds;
            Rect3D combinedRectBounds = Rect3D.Union(structure1Bounds, structure2Bounds);

            // to allow the resolution to be on the same scale in each direction
            double startZ = Math.Floor(combinedRectBounds.Z - 1);
            double endZ = (startZ + Math.Round(combinedRectBounds.SizeZ + 2));
            double startX = Math.Floor(combinedRectBounds.X - 1);
            double endX = (startX + Math.Round(combinedRectBounds.SizeX + 2));
            double startY = Math.Floor(combinedRectBounds.Y - 1);
            double endY = (startY + Math.Round(combinedRectBounds.SizeY + 2));

            if (structure1Bounds.Contains(structure2Bounds))
            {
                volumeIntersection = structure2.Volume;
            }
            else if (structure2Bounds.Contains(structure1Bounds))
            {
                volumeIntersection = structure1.Volume;
            }
            // using the bounds of each rectangle as the ROI for calculating overlap
            else
            {
                for (double z = startZ; z < endZ; z += .5)
                {
                    //planDose.GetVoxels(z, dosePlaneVoxels);
                    for (double y = startY; y < endY; y += 1)
                    {
                        for (double x = startX; x < endX; x += 1)
                        {
                            p.x = x;
                            p.y = y;
                            p.z = z;

                            if ((structure2Bounds.Contains(p.x, p.y, p.z)) &&
                                (structure1.IsPointInsideSegment(p)) &&
                                (structure2.IsPointInsideSegment(p)))
                            {
                                intersectionCount++;
                            }
                            volumeIntersection = (intersectionCount * 0.001 * .5);
                        }
                    }
                }
            }
            return volumeIntersection;
        }
        public static double PercentOverlap(Structure structure, double volumeIntersection)
        {
            double percentOverlap = (volumeIntersection / structure.Volume) * 100;
            if (percentOverlap > 100)
            {
                percentOverlap = 100;
                return percentOverlap;
            }
            else
            {
                return percentOverlap;
            }
        }
        public static double DiceCoefficient(Structure structure1, Structure structure2)
        {
            // initialize items needed for calculating distance
            VVector p = new VVector();
            double volumeIntersection = 0;
            double volumeStructure1 = 0;
            double volumeStructure2 = 0;
            int intersectionCount = 0;
            int structure1Count = 0;
            int structure2Count = 0;
            double diceCoefficient = 0;

            Rect3D structure1Bounds = structure1.MeshGeometry.Bounds;
            Rect3D structure2Bounds = structure2.MeshGeometry.Bounds;
            Rect3D combinedRectBounds = Rect3D.Union(structure1Bounds, structure2Bounds);
            // to allow the resolution to be on the same scale in each direction
            double startZ = Math.Floor(combinedRectBounds.Z - 1);
            double endZ = (startZ + Math.Round(combinedRectBounds.SizeZ + 2));
            double startX = Math.Floor(combinedRectBounds.X - 1);
            double endX = (startX + Math.Round(combinedRectBounds.SizeX + 2));
            double startY = Math.Floor(combinedRectBounds.Y - 1);
            double endY = (startY + Math.Round(combinedRectBounds.SizeY + 2));

            if (structure1 != structure2)
            {

                if (structure1Bounds.Contains(structure2Bounds))
                {
                    volumeIntersection = structure2.Volume;
                    volumeStructure1 = structure1.Volume;
                    volumeStructure2 = structure2.Volume;
                }
                else if (structure2Bounds.Contains(structure1Bounds))
                {
                    volumeIntersection = structure1.Volume;
                    volumeStructure1 = structure1.Volume;
                    volumeStructure2 = structure2.Volume;
                }
                else
                {
                    // using the bounds of each rectangle as the ROI for calculating overlap
                    for (double z = startZ; z < endZ; z += .5)
                    {
                        for (double y = startY; y < endY; y += 1)
                        {
                            for (double x = startX; x < endX; x += 1)
                            {
                                p.x = x;
                                p.y = y;
                                p.z = z;

                                if ((structure2Bounds.Contains(p.x, p.y, p.z)) &&
                                    (structure1.IsPointInsideSegment(p)) &&
                                    (structure2.IsPointInsideSegment(p)))
                                {
                                    intersectionCount++;
                                }
                                if (structure1.IsPointInsideSegment(p))
                                {
                                    structure1Count++;
                                }
                                if (structure2.IsPointInsideSegment(p))
                                {
                                    structure2Count++;
                                }
                                volumeIntersection = (intersectionCount * 0.001 * .5);
                                volumeStructure1 = (structure1Count * 0.001 * .5);
                                volumeStructure2 = (structure2Count * 0.001 * .5);
                            }
                        }
                    }
                }
                diceCoefficient = Math.Round(((2 * volumeIntersection) / (volumeStructure1 + volumeStructure2)), 3);
                return diceCoefficient;
            }
            else
            {
                diceCoefficient = 1;
                return diceCoefficient;
            }
        }
        public static double DiceCoefficient(Structure structure1, Structure structure2, double volumeOverlap)
        {
            return Math.Round((2 * volumeOverlap) / (structure1.Volume + structure2.Volume), 3);
        }
        public static double ShortestDistance(Structure structure1, Structure structure2)
        {
            // calculate the shortest distance between each structure
            Rect3D structure1Bounds = structure1.MeshGeometry.Bounds;
            Rect3D structure2Bounds = structure2.MeshGeometry.Bounds;
            Point3DCollection vertexesStructure1 = new Point3DCollection();
            Point3DCollection vertexesStructure2 = new Point3DCollection();
            vertexesStructure1 = structure1.MeshGeometry.Positions;
            vertexesStructure2 = structure2.MeshGeometry.Positions;
            double shortestDistance = 2000000;
            if (structure1 != structure2)
            {
                if (structure1Bounds.Contains(structure2Bounds))
                {
                    shortestDistance = 0;
                    return shortestDistance;
                }
                else if (structure2Bounds.Contains(structure1Bounds))
                {
                    shortestDistance = 0;
                    return shortestDistance;
                }
                else
                {
                    foreach (Point3D v1 in vertexesStructure1)
                    {
                        foreach (Point3D v2 in vertexesStructure2)
                        {
                            double distance = (Math.Sqrt((Math.Pow((v2.X - v1.X), 2)) + (Math.Pow((v2.Y - v1.Y), 2)) + (Math.Pow((v2.Z - v1.Z), 2)))) / 10;
                            if (distance < shortestDistance)
                            {
                                shortestDistance = distance;
                            }
                        }
                    }
                    return shortestDistance;
                }
            }
            else
            {
                shortestDistance = 0;
                return shortestDistance;
            }
        }
        public static double MaxDistance(Structure structure1, Structure structure2)
        {
            // calculate the max distance between each structure
            Rect3D structure1Bounds = structure1.MeshGeometry.Bounds;
            Rect3D structure2Bounds = structure2.MeshGeometry.Bounds;
            Point3DCollection vertexesStructure1 = new Point3DCollection();
            Point3DCollection vertexesStructure2 = new Point3DCollection();
            vertexesStructure1 = structure1.MeshGeometry.Positions;
            vertexesStructure2 = structure2.MeshGeometry.Positions;
            double maxDistance = 0;

            if (structure1Bounds.Contains(structure2Bounds))
            {
                maxDistance = 0;
                return maxDistance;
            }
            else if (structure2Bounds.Contains(structure1Bounds))
            {
                maxDistance = 0;
                return maxDistance;
            }
            else
            {
                foreach (Point3D v1 in vertexesStructure1)
                {
                    foreach (Point3D v2 in vertexesStructure2)
                    {
                        double distance = (Math.Sqrt((Math.Pow((v2.X - v1.X), 2)) + (Math.Pow((v2.Y - v1.Y), 2)) + (Math.Pow((v2.Z - v1.Z), 2)))) / 10;
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                        }
                    }
                }
                return maxDistance;
            }
        }
        #region Average Distances inside/outside radius -- may not be useful
        public static double AverageDistance_InsideRadius(Structure structure1, Structure structure2, double radius)
        {
            // calculate the average distance between each structure inside a designated radius
            List<double> pointsInsideRadius = new List<double>();
            Rect3D structure1Bounds = structure1.MeshGeometry.Bounds;
            Rect3D structure2Bounds = structure2.MeshGeometry.Bounds;
            Point3DCollection vertexesStructure1 = new Point3DCollection();
            Point3DCollection vertexesStructure2 = new Point3DCollection();
            vertexesStructure1 = structure1.MeshGeometry.Positions;
            vertexesStructure2 = structure2.MeshGeometry.Positions;
            double averageDistance = 0;

            if (structure1Bounds.Contains(structure2Bounds))
            {
                averageDistance = 0;
                return averageDistance;
            }
            else if (structure2Bounds.Contains(structure1Bounds))
            {
                averageDistance = 0;
                return averageDistance;
            }
            else
            {
                foreach (Point3D v1 in vertexesStructure1)
                {
                    foreach (Point3D v2 in vertexesStructure2)
                    {
                        double distance = (Math.Sqrt((Math.Pow((v2.X - v1.X), 2)) + (Math.Pow((v2.Y - v1.Y), 2)) + (Math.Pow((v2.Z - v1.Z), 2)))) / 10;
                        if (distance <= radius)
                        {
                            pointsInsideRadius.Add(distance);
                        }
                    }
                }
                if (pointsInsideRadius.Count == 0)
                {
                    averageDistance = 0;
                }
                else
                {
                    averageDistance = pointsInsideRadius.Average();
                }
                return averageDistance;
            }
        }
        public static double AverageDistance_OutsideRadius(Structure structure1, Structure structure2, double radius)
        {
            List<double> pointsOutsideRadius = new List<double>();
            Rect3D structure1Bounds = structure1.MeshGeometry.Bounds;
            Rect3D structure2Bounds = structure2.MeshGeometry.Bounds;
            // calculate the average distance between each structure outside a designated radius
            Point3DCollection vertexesStructure1 = new Point3DCollection();
            Point3DCollection vertexesStructure2 = new Point3DCollection();
            vertexesStructure1 = structure1.MeshGeometry.Positions;
            vertexesStructure2 = structure2.MeshGeometry.Positions;
            double averageDistance = 0;

            if (structure1Bounds.Contains(structure2Bounds))
            {
                averageDistance = 0;
                return averageDistance;
            }
            else if (structure2Bounds.Contains(structure1Bounds))
            {
                averageDistance = 0;
                return averageDistance;
            }
            else
            {
                foreach (Point3D v1 in vertexesStructure1)
                {
                    //VVector p1 = new VVector();
                    foreach (Point3D v2 in vertexesStructure2)
                    {
                        double distance = (Math.Sqrt((Math.Pow((v2.X - v1.X), 2)) + (Math.Pow((v2.Y - v1.Y), 2)) + (Math.Pow((v2.Z - v1.Z), 2)))) / 10;
                        if (distance > radius)
                        {
                            pointsOutsideRadius.Add(distance);
                        }
                    }
                }
                if (pointsOutsideRadius.Count == 0)
                {
                    averageDistance = 0;
                }
                else
                {
                    averageDistance = pointsOutsideRadius.Average();
                }
                return averageDistance;
            }
        }
        #endregion avg distances
    }
    #endregion Class Definitions

    public class Script
    {
        public Script()
        {
        }
        //---------------------------------------------------------------------------------------------  
        #region execute

        public void Execute(ScriptContext context, Window window)
        {
            PlanSetup plan = context.PlanSetup;
            double planPrescribedPercentage = plan.TreatmentPercentage;
            PlanSum psum = context.PlanSumsInScope.FirstOrDefault();
            if (context.PlanSetup == null && context.PlanSumsInScope.Count() > 1)
            {
                throw new ApplicationException("Please close other plan sums");
            }
            if (plan == null && psum == null)
                return;

            window.Closing += new System.ComponentModel.CancelEventHandler(OnWindowClosing);
            window.Background = System.Windows.Media.Brushes.Cornsilk;
            //window.Width = 750;
            //window.SizeToContent = SizeToContent.Height;
            window.SizeToContent = SizeToContent.WidthAndHeight;
            
            SelectedPlanningItem = plan != null ? (PlanningItem)plan : (PlanningItem)psum;
            // NOTE: Plans in plansum can have different structuresets but here we only use structureset to allow chosing one structure
            SelectedStructureSet = plan != null ? plan.StructureSet : psum.PlanSetups.First().StructureSet;

            window.Title = "DVH Lookups for " + SelectedPlanningItem.Id + " / " + SelectedStructureSet.Id;

            // NOTE: this will cause the script to close if there is no dose calculated
            // removed so overlap can be calculated before running a plan
            //if (SelectedPlanningItem.Dose == null)
            //    return;

            InitializeUI(window);
        }

        #endregion execute
        //---------------------------------------------------------------------------------------------  
        #region closing

        bool m_closing = false;

        void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_closing = true;
        }
        #endregion closing
        //---------------------------------------------------------------------------------------------  
        #region Window / Stackboxes

        void InitializeUI(Window window)
        {
            StackPanel rootPanel = new StackPanel();
            rootPanel.Orientation = Orientation.Vertical;
            //rootPanel.Height = 1024;

            // NOTE: Structure1 stackbox
            {
                GroupBox structureGroup = new GroupBox();
                structureGroup.Header = "Structure 1";
                rootPanel.Children.Add(structureGroup);

                StackPanel structurePanel = new StackPanel();
                structurePanel.Orientation = Orientation.Horizontal;
                structurePanel.VerticalAlignment = VerticalAlignment.Center;

                ComboBox structureCombo = new ComboBox();
                // NOTE: excludes pt markers and empty structures (e.g., iso markers and structures not contoured)
                foreach (Structure structure in SelectedStructureSet.Structures)
                {
                    if ((structure.HasSegment == true) && (structure.IsEmpty == false))
                    {
                        structureCombo.Items.Add(structure);
                    }
                }
                // NOTE: or if you'd rather just include everything, you can use the original code
                //structureCombo.ItemsSource = SelectedStructureSet.Structures;

                structureCombo.SelectionChanged += OnComboSelectionChanged;
                structureCombo.MinWidth = 175;

                Label volumeLabel = new Label();
                volumeLabel.Content = "Volume (cm3)";
                volumeLabel.VerticalAlignment = VerticalAlignment.Center;
                m_structureVolume.VerticalAlignment = VerticalAlignment.Center;

                structureGroup.Content = structurePanel;

                structurePanel.Children.Add(structureCombo);
                structurePanel.Children.Add(volumeLabel);
                structurePanel.Children.Add(m_structureVolume);

                m_absDoseCheckbox.Content = "AbsDose ";
                m_absDoseCheckbox.VerticalAlignment = VerticalAlignment.Center;
                m_absDoseCheckbox.Checked += new RoutedEventHandler(CheckBoxChanged);
                m_absDoseCheckbox.Unchecked += new RoutedEventHandler(CheckBoxChanged);
                m_absDoseCheckbox.IsChecked = true;
                m_absVolCheckbox.IsChecked = true;
                if (SelectedPlanningItem is PlanSum)
                {
                    // NOTE: only absolute dose for plansums
                    m_absDoseCheckbox.IsChecked = true;
                    m_absDoseCheckbox.IsChecked = true;
                }
                structurePanel.Children.Add(m_absDoseCheckbox);

                m_absVolCheckbox.Content = "AbsVol";
                m_absVolCheckbox.VerticalAlignment = VerticalAlignment.Center;
                m_absVolCheckbox.Checked += new RoutedEventHandler(CheckBoxChanged);
                m_absVolCheckbox.Unchecked += new RoutedEventHandler(CheckBoxChanged);
                structurePanel.Children.Add(m_absVolCheckbox);
            }
            // NOTE: DVH lookup stackbox
            {
                GroupBox dvhGroup = new GroupBox();
                dvhGroup.Header = "DVH";
                rootPanel.Children.Add(dvhGroup);

                StackPanel dvhPanel = new StackPanel();
                dvhPanel.Orientation = Orientation.Horizontal;

                m_volumeTextBox.TextChanged += new TextChangedEventHandler(OnInputChanged);
                m_doseTextBox.TextChanged += new TextChangedEventHandler(OnInputChanged);

                dvhGroup.Content = dvhPanel;

                m_volumeAtDoseLabel.Content = "Volume at Dose";
                m_volumeAtDoseLabel.VerticalAlignment = VerticalAlignment.Center;

                m_doseAtVolumeLabel.Content = "Dose at Volume";
                m_doseAtVolumeLabel.VerticalAlignment = VerticalAlignment.Center;

                dvhPanel.Children.Add(m_volumeAtDoseLabel);
                dvhPanel.Children.Add(m_doseTextBox);
                dvhPanel.Children.Add(m_volumeAtDoseResultLabel);
                dvhPanel.Children.Add(m_resultVolumeAtDose);

                dvhPanel.Children.Add(m_doseAtVolumeLabel);
                dvhPanel.Children.Add(m_volumeTextBox);
                dvhPanel.Children.Add(m_doseAtVolumeResultLabel);
                dvhPanel.Children.Add(m_resultDoseAtVolume);
            }
            // NOTE: Ratio stackbox
            {
                GroupBox CICalc = new GroupBox();
                CICalc.Header = "CI/R50 Calc";
                rootPanel.Children.Add(CICalc);

                StackPanel ciPanel = new StackPanel();
                ciPanel.Orientation = Orientation.Horizontal;

                // NOTE: these are used in case you choose to utilize user input for ratio calcs
                //m_volPITextBox.TextChanged += new TextChangedEventHandler(OnInputChanged);
                //m_volPTVTextBox.TextChanged += new TextChangedEventHandler(OnInputChanged);

                CICalc.Content = ciPanel;

                m_volPILabel.Content = "V1";
                m_volPILabel.VerticalAlignment = VerticalAlignment.Center;

                m_PIVolume.Content = "";
                m_PIVolume.VerticalAlignment = VerticalAlignment.Center;

                m_volPIResultLabel.Content = "cc    / ";
                m_volPIResultLabel.VerticalAlignment = VerticalAlignment.Center;

                m_volPTVLabel.Content = "V2";
                m_volPTVLabel.VerticalAlignment = VerticalAlignment.Center;

                m_PTVVolume.Content = "";
                m_PTVVolume.VerticalAlignment = VerticalAlignment.Center;

                m_volPTVResultLabel.Content = "cc   = ";
                m_volPTVResultLabel.VerticalAlignment = VerticalAlignment.Center;

                m_R50RangeLabel.Content = "R50 Range:";
                m_R50RangeLabel.VerticalAlignment = VerticalAlignment.Center;
                
                ciPanel.Children.Add(m_volPILabel);
                //ciPanel.Children.Add(m_volPITextBox);
                ciPanel.Children.Add(m_PIVolume);
                ciPanel.Children.Add(m_volPIResultLabel);

                ciPanel.Children.Add(m_volPTVLabel);
                //ciPanel.Children.Add(m_volPTVTextBox);
                ciPanel.Children.Add(m_PTVVolume);
                ciPanel.Children.Add(m_volPTVResultLabel);

                ciPanel.Children.Add(m_resultCI);

                ciPanel.Children.Add(m_R50RangeLabel);
                ciPanel.Children.Add(m_R50Range);
                ciPanel.Children.Add(m_R50Result);
            }
            // NOTE: Structure2/overlap stackbox
            {
                GroupBox Structure2 = new GroupBox();
                Structure2.Header = "Structure 2";
                rootPanel.Children.Add(Structure2);

                StackPanel structure2Panel = new StackPanel();
                structure2Panel.Orientation = Orientation.Horizontal;

                ComboBox structureCombo2 = new ComboBox();
                // NOTE: excludes pt markers and empty structures (e.g., iso markers and structures not contoured)
                foreach (Structure structure in SelectedStructureSet.Structures)
                {
                    if ((structure.HasSegment == true) && (structure.IsEmpty == false))
                    {
                        structureCombo2.Items.Add(structure);
                    }
                }
                // NOTE: or if you'd rather just include everything, you can use the original code
                //structureCombo2.ItemsSource = SelectedStructureSet.Structures;
                structureCombo2.SelectionChanged += OnComboSelectionChanged2;
                structureCombo2.MinWidth = 175;

                Label volumeLabel2 = new Label();
                volumeLabel2.Content = "Volume (cm3)";
                volumeLabel2.VerticalAlignment = VerticalAlignment.Center;
                m_structureVolume2.VerticalAlignment = VerticalAlignment.Center;

                structure2Panel.Children.Add(structureCombo2);
                structure2Panel.Children.Add(volumeLabel2);
                structure2Panel.Children.Add(m_structureVolume2);

                Structure2.Content = structure2Panel;

                Label volOverlapLabel = new Label();
                volOverlapLabel.Content = "Overlap:";
                volOverlapLabel.VerticalAlignment = VerticalAlignment.Center;

                structure2Panel.Children.Add(volOverlapLabel);
                structure2Panel.Children.Add(m_volOverlap);
                structure2Panel.Children.Add(m_volOverlapResult);
            }
            {
                GroupBox Distance = new GroupBox();
                Distance.Header = "Distance Information";
                rootPanel.Children.Add(Distance);

                StackPanel distancePanel = new StackPanel();
                distancePanel.Orientation = Orientation.Horizontal;

                Distance.Content = distancePanel;

                distancePanel.Children.Add(m_distanceLabel);
                distancePanel.Children.Add(m_distanceResult);
                distancePanel.Children.Add(m_maxDistanceLabel);
                // distancePanel.Children.Add(m_diceCoefficientLabel);
                distancePanel.Children.Add(m_avgDistance_1cm_Label);
                distancePanel.Children.Add(m_avgDistance_1cm_Result);
                distancePanel.Children.Add(m_avgDistance_2cm_Label);
                distancePanel.Children.Add(m_avgDistance_2cm_Result);
            }
            // NOTE: Layout
            {
                m_structureVolume.MinWidth = 60.0;
                m_structureVolume.VerticalAlignment = VerticalAlignment.Center;

                m_volumeTextBox.MinWidth = 60.0;
                m_volumeTextBox.VerticalAlignment = VerticalAlignment.Center;

                m_doseTextBox.MinWidth = 60.0;
                m_doseTextBox.VerticalAlignment = VerticalAlignment.Center;

                //m_volPITextBox.MinWidth = 60.0;
                //m_volPITextBox.VerticalAlignment = VerticalAlignment.Center;

                //m_volPTVTextBox.MinWidth = 60.0;
                //m_volPTVTextBox.VerticalAlignment = VerticalAlignment.Center;

                m_PIVolume.MinWidth = 25.0;
                m_PIVolume.VerticalAlignment = VerticalAlignment.Center;

                m_PTVVolume.MinWidth = 25.0;
                m_PTVVolume.VerticalAlignment = VerticalAlignment.Center;

                m_resultVolumeAtDose.MinWidth = 60.0;
                m_resultVolumeAtDose.VerticalAlignment = VerticalAlignment.Center;

                m_resultDoseAtVolume.MinWidth = 60.0;
                m_resultDoseAtVolume.VerticalAlignment = VerticalAlignment.Center;

                m_resultCI.MinWidth = 60.0;
                m_resultCI.VerticalAlignment = VerticalAlignment.Center;

                m_R50RangeLabel.MinWidth = 60.0;
                m_R50RangeLabel.VerticalAlignment = VerticalAlignment.Center;

                m_R50Range.MinWidth = 100.0;
                m_R50Range.VerticalAlignment = VerticalAlignment.Center;

                m_R50Result.MinWidth = 160.0;
                m_R50Result.VerticalAlignment = VerticalAlignment.Center;

                m_volOverlap.MinWidth = 30.0;
                m_volOverlap.VerticalAlignment = VerticalAlignment.Center;

                m_volOverlapResult.MinWidth = 25.0;
                m_volOverlapResult.VerticalAlignment = VerticalAlignment.Center;

                m_avgDistance_1cm_Label.MinWidth = 15.0;
                m_avgDistance_1cm_Label.VerticalAlignment = VerticalAlignment.Center;

                m_avgDistance_1cm_Result.MinWidth = 20.0;
                m_avgDistance_1cm_Result.VerticalAlignment = VerticalAlignment.Center;

                m_avgDistance_2cm_Label.MinWidth = 15.0;
                m_avgDistance_2cm_Label.VerticalAlignment = VerticalAlignment.Center;

                m_avgDistance_2cm_Result.MinWidth = 20.0;
                m_avgDistance_2cm_Result.VerticalAlignment = VerticalAlignment.Center;

                m_maxDistanceLabel.MinWidth = 15.0;
                m_maxDistanceLabel.VerticalAlignment = VerticalAlignment.Center;

               m_diceCoefficientLabel.MinWidth = 20.0;
                m_diceCoefficientLabel.VerticalAlignment = VerticalAlignment.Center;

                m_distanceLabel.MinWidth = 15.0;
                m_distanceLabel.VerticalAlignment = VerticalAlignment.Center;

                m_distanceResult.MinWidth = 20.0;
                m_distanceResult.VerticalAlignment = VerticalAlignment.Center;

                m_structureVolume2.MinWidth = 60.0;
                m_structureVolume2.VerticalAlignment = VerticalAlignment.Center;

                rootPanel.VerticalAlignment = VerticalAlignment.Center;

                Thickness myThickness = new Thickness();
                myThickness.Top = 25;
                myThickness.Bottom = 35;
                myThickness.Left = 35;
                myThickness.Right = myThickness.Left;

                rootPanel.Margin = myThickness;
            }
            window.Content = rootPanel;
        }
        #endregion window
        //---------------------------------------------------------------------------------------------  
        #region update after checking/unchecking boxes

        void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            UpdateDvhLookup();
        }

        #endregion update: box change
        //---------------------------------------------------------------------------------------------  
        #region initializing items

        // NOTE: initializing items used
        PlanningItem SelectedPlanningItem { get; set; }
        StructureSet SelectedStructureSet { get; set; }
        Structure SelectedStructure { get; set; }
        Structure SelectedStructure2 { get; set; }

        TextBlock m_structureVolume = new TextBlock();
        TextBlock m_structureVolume2 = new TextBlock();

        TextBox m_volumeTextBox = new TextBox();
        Label m_resultVolumeAtDose = new Label();
        TextBox m_doseTextBox = new TextBox();
        Label m_resultDoseAtVolume = new Label();
        static CheckBox m_absDoseCheckbox = new CheckBox();
        static CheckBox m_absVolCheckbox = new CheckBox();
        Label m_doseAtVolumeLabel = new Label();
        Label m_volumeAtDoseLabel = new Label();
        
        Label m_volPILabel = new Label();
        Label m_volPTVLabel = new Label();
        //TextBox m_volPITextBox = new TextBox();
        //TextBox m_volPTVTextBox = new TextBox();
        Label m_PIVolume = new Label();
        Label m_PTVVolume = new Label();
        Label m_resultCI = new Label();
        
        Label m_doseAtVolumeResultLabel = new Label();
        Label m_volumeAtDoseResultLabel = new Label();

        Label m_volPIResultLabel = new Label();
        Label m_volPTVResultLabel = new Label();

        Label m_R50RangeLabel = new Label();
        Label m_R50RangeResultLabel = new Label();
        Label m_R50Range = new Label();
        Label m_R50Result = new Label();

        Label m_volOverlap = new Label();
        Label m_volOverlapResult = new Label();

        Label m_distanceLabel = new Label();
        Label m_distanceResult = new Label();

        Label m_avgDistance_1cm_Label = new Label();
        Label m_avgDistance_1cm_Result = new Label();

        Label m_avgDistance_2cm_Label = new Label();
        Label m_avgDistance_2cm_Result = new Label();

        Label m_maxDistanceLabel = new Label();
        Label m_diceCoefficientLabel = new Label();

        Rect3D selectedStructure1_Bounds = new Rect3D();
        Rect3D selectedStructure2_Bounds = new Rect3D();

        #endregion initializing items
        //---------------------------------------------------------------------------------------------  
        #region update on input change

        void OnInputChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDvhLookup();
        }
        #endregion update on input change
        //---------------------------------------------------------------------------------------------
        #region UpdateDVHLookup()

        static double s_binWidth = 0.001;

        void UpdateDvhLookup()
        {
            bool doseAbsolute = m_absDoseCheckbox.IsChecked.Value;
            bool volAbsolute = m_absVolCheckbox.IsChecked.Value;

            DoseValuePresentation dosePres = doseAbsolute ? DoseValuePresentation.Absolute : DoseValuePresentation.Relative;
            VolumePresentation volPres = volAbsolute ? VolumePresentation.AbsoluteCm3 : VolumePresentation.Relative;

            DVHData dvhData = SelectedPlanningItem.GetDVHCumulativeData(SelectedStructure, dosePres, volPres, s_binWidth);
            DVHData dvhData2 = SelectedPlanningItem.GetDVHCumulativeData(SelectedStructure2, dosePres, volPres, s_binWidth);

            // NOTE: initialize variables used below
            m_structureVolume.Text = "";
            m_volumeAtDoseResultLabel.Content = "";
            m_doseAtVolumeResultLabel.Content = "";
            m_resultVolumeAtDose.Content = "";
            m_resultDoseAtVolume.Content = "";

            // NOTE: conditions for different sections to work
            if (m_closing == false && (SelectedStructure != null || SelectedStructure2 != null))
            {
                // NOTE: portions that will work if dose not calculated
                if (SelectedStructure != null && SelectedPlanningItem.Dose == null)
                {
                    m_structureVolume.Text = SelectedStructure.Volume.ToString("F3");
                }
                // NOTE: portions that will work if dose is calculated
                if (SelectedStructure != null && SelectedPlanningItem.Dose != null)
                {
                    m_structureVolume.Text = SelectedStructure.Volume.ToString("F3");

                    double inputVolume = Double.NaN;
                    if (m_volumeTextBox.Text != null)
                    {
                        Double.TryParse(m_volumeTextBox.Text, out inputVolume);
                    }
                    double inputDose = Double.NaN;
                    if (m_doseTextBox.Text != null)
                    {
                        Double.TryParse(m_doseTextBox.Text, out inputDose);
                    }
                    // NOTE: these were used to ask for input to calculate ratios
                    //double inputPTVVolume = Double.NaN;
                    //if (m_volPTVTextBox.Text != null)
                    //{
                    //    Double.TryParse(m_volPTVTextBox.Text, out inputPTVVolume);
                    //}
                    //double inputVpi = Double.NaN;
                    //if (m_volPITextBox.Text != null)
                    //{
                    //    Double.TryParse(m_volPITextBox.Text, out inputVpi);
                    //}
                    if (!Double.IsNaN(inputVolume))
                    {
                        DoseValue val = SelectedPlanningItem.GetDoseAtVolume(SelectedStructure, inputVolume, volPres, dosePres);
                        //DoseValue controlVal = DvhExtensions.DoseAtVolume(dvhData, inputVolume);
                        //double err = Math.Abs((val.Dose - controlVal.Dose) / val.Dose);
                        //if (err > 0.001)
                        //{
                        //    MessageBox.Show("Value : " + val.ToString() + " Control Val : " + controlVal.ToString());
                        //}
                        m_resultDoseAtVolume.Content = val.ToString();

                        string doseAtVolumeResultType = volAbsolute ? "cm3 D(" : "% D(";
                        doseAtVolumeResultType += inputVolume.ToString("F2") + (volAbsolute ? "cm3" : "%") + ") =";
                        m_doseAtVolumeResultLabel.Content = doseAtVolumeResultType;
                        m_doseAtVolumeResultLabel.VerticalAlignment = VerticalAlignment.Center;
                    }
                    if (!Double.IsNaN(inputDose))
                    {
                        DoseValue.DoseUnit doseUnit = dvhData.MaxDose.Unit;
                        double vol = SelectedPlanningItem.GetVolumeAtDose(SelectedStructure, new DoseValue(inputDose, doseUnit), volPres);

                        //double controlVal = DvhExtensions.VolumeAtDose(dvhData, inputDose);
                        //ouble err = Math.Abs((vol - controlVal) / vol);

                        //if (err > 0.001)
                        //{
                        //    MessageBox.Show("Value : " + vol.ToString("F3") + " Control Val : " + controlVal.ToString("F3"));
                        //}
                        if (SelectedStructure2 != null)
                        {
                            double ratio = vol / SelectedStructure2.Volume;
                            m_PIVolume.Content = vol.ToString("F3");
                            m_PTVVolume.Content = SelectedStructure2.Volume.ToString("F3");
                            m_resultCI.Content = ratio.ToString("F2");

                            double limit1, limit2, limit3, limit4;
                            R50Constraint.LimitsFromVolume(SelectedStructure2.Volume, out limit1, out limit2, out limit3, out limit4);
                            m_R50Range.Content = limit1.ToString("F2") + " to " + limit2.ToString("F2");
                            if (ratio <= limit1)
                            {
                                m_R50Result.Content = "R50: Per Protocol";
                            }
                            if (limit1 < ratio && ratio <= limit2)
                            {
                                m_R50Result.Content = "R50: Acceptable per RTOG 0915";
                            }
                            if (ratio > limit2)
                            {
                                m_R50Result.Content = "R50: Unacceptable per RTOG 0915";
                            }

                        }
                        m_resultVolumeAtDose.Content = vol.ToString("F3") + (volAbsolute ? "cm3" : "%");

                        string volumeAtDoseResultType = doseUnit.ToString() + " V(";
                        volumeAtDoseResultType += inputDose.ToString("F2") + doseUnit.ToString() + " ) =";
                        m_volumeAtDoseResultLabel.Content = volumeAtDoseResultType;
                        m_volumeAtDoseResultLabel.VerticalAlignment = VerticalAlignment.Center;
                    }
                    // NOTE: these calculate ratios based on input rather than values already calculated
                    //if (!Double.IsNaN(inputVpi) && !Double.IsNaN(inputPTVVolume))
                    //{
                    //    double ratio = inputVpi / inputPTVVolume;
                    //    //double ratio = vol / SelectedStructure2.Volume;

                    //    m_resultCI.Content = ratio.ToString("F2");
                    //}
                    //double limit1, limit2, limit3, limit4;
                    //R50Constraint.LimitsFromVolume(inputPTVVolume, out limit1, out limit2, out limit3, out limit4);
                    //if (!Double.IsNaN(inputPTVVolume))
                    //{
                    //    m_R50Range.Content = limit1.ToString("F2") + " to " + limit2.ToString("F2");
                    //}
                }
                return;
            }
        }
        #endregion Update DVH Lookup
        //---------------------------------------------------------------------------------------------  
        #region UpdateDistanceCalc()
            
        void UpdateDistanceCalc()
        {
            //PlanSetup plan = (PlanSetup)SelectedPlanningItem;

            m_structureVolume2.Text = "";
            m_volOverlap.Content = "";
            m_distanceLabel.Content = "";
            m_diceCoefficientLabel.Content = "";

            // NOTE: not currently needed
            //m_volOverlapResult.Content = "";
            //m_distanceResult.Content = "";
            //m_avgDistance_1cm_Label.Content = "";
            //m_avgDistance_1cm_Result.Content = "";
            //m_avgDistance_2cm_Label.Content = "";
            //m_avgDistance_2cm_Result.Content = "";


            // NOTE: conditions for different sections to work
            if (m_closing == false && (SelectedStructure != null || SelectedStructure2 != null))
            {
                // NOTE: portions that will work if second structure selected
                if (SelectedStructure2 != null)
                {
                    m_structureVolume2.Text = SelectedStructure2.Volume.ToString("F3");

                    // NOTE: portions that will work when both structures are selected
                    if (SelectedStructure != null && SelectedStructure2 != null)
                    {
                        double volumeIntersection = 0;
                        double diceCoefficient = 0;

                        // NOTE: initialize items needed for calculating distance
                        double shortestDistance = 0;
                        double percentOverlap = 0;
                        
                        if (SelectedStructure2 != null)
                        {
                            selectedStructure2_Bounds = SelectedStructure2.MeshGeometry.Bounds;
                        }
                        if (SelectedStructure != null)
                        {
                            selectedStructure1_Bounds = SelectedStructure.MeshGeometry.Bounds;
                        }
                        
                        volumeIntersection = CalculateOverlap.VolumeOverlap(SelectedStructure, SelectedStructure2);
                        percentOverlap = CalculateOverlap.PercentOverlap(SelectedStructure2, volumeIntersection);
                        shortestDistance = CalculateOverlap.ShortestDistance(SelectedStructure, SelectedStructure2);
                        diceCoefficient = CalculateOverlap.DiceCoefficient(SelectedStructure, SelectedStructure2, volumeIntersection);
                        
                        m_volOverlap.Content = volumeIntersection.ToString("F3") + /*" \u00B1 0.05cc*/ " (" + string.Format("{0:F1}%)", percentOverlap);
                        m_distanceLabel.Content = string.Format("Shortest Distance = {0:F1} cm", shortestDistance);
                        m_diceCoefficientLabel.Content = "Dice Coefficient = " + diceCoefficient;

                        #region TODO -- look for meaningful representation of average and/or max distance away from a structure

                        // NOTE: currently working on method that will calculate the volume of a structure within a certain distance/radius;
                        //          should be more useful than avg distance
                    //    double maxDistance = 0;
//maxDistance = CalculateOverlap.MaxDistance(SelectedStructure, SelectedStructure2);
                        // double averageDistance_Outside1cm = 0;
                        // double averageDistance_Outside2cm = 0;

                        //m_maxDistanceLabel.Content = "Max Distance = " + string.Format("{0:F1} cm", maxDistance);
                        //if (percentOverlap > 0)
                        //{
                        //if (maxDistance > averageDistance_Outside1cm)
                        //{
                          //  m_avgDistance_1cm_Label.Content = "Avg Distance (> 1cm radius) = " + string.Format("{0:F1} cm", averageDistance_Outside1cm);
//
                         // if (averageDistance_Outside1cm != averageDistance_Outside2cm)
                        //   {
                          //     m_avgDistance_2cm_Label.Content = "Avg Distance (> 2cm radius) = " + string.Format("{0:F1} cm", averageDistance_Outside2cm);
                         //  }
                      //  }
                        #endregion
                    }
                }
                return;
            }
        }
#endregion update distance calc
        //---------------------------------------------------------------------------------------------  
        #region remaining updates: combobox selections and percentage text change

        private void OnComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox senderComboBox = (ComboBox)sender;

            // Change the length of the text box depending on what the user has 
            // selected and committed using the SelectionLength property.
            if (senderComboBox.SelectedIndex >= 0)
            {
                SelectedStructure = senderComboBox.Items[senderComboBox.SelectedIndex] as Structure;
                UpdateDvhLookup();
                UpdateDistanceCalc();
            }
        }
        private void OnComboSelectionChanged2(object sender, SelectionChangedEventArgs e)
        {
            ComboBox senderComboBox2 = (ComboBox)sender;

            if (senderComboBox2.SelectedIndex >= 0)
            {
                SelectedStructure2 = senderComboBox2.Items[senderComboBox2.SelectedIndex] as Structure;
                UpdateDvhLookup();
                UpdateDistanceCalc();
            }
        }
        private void OnPercentageTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDvhLookup();
        }
        #endregion remaining updates
        //---------------------------------------------------------------------------------------------  
    }
}
