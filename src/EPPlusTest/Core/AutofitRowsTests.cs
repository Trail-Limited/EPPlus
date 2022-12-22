﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPPlusTest.Core
{
    [TestClass]
    public class AutofitRowsTests : TestBase
    {

        [TestMethod]
        public void AutofitRow_ShouldCalculateNewRowHeightWhenWrapTextIsTrue()
        {
            using (var pck = OpenPackage("AutofitRows_DefaultWidth_WrapText_True.xlsx", true))
            {
                var sheet = pck.Workbook.Worksheets.Add("Sheet1");
                sheet.Cells["A1"].Value = "A long text that needs some serious autofit of row height";
                sheet.Cells["A1"].Style.WrapText = true;
                sheet.Cells["A1"].AutoFitRows();
                Assert.AreEqual(122.2d, sheet.Row(1).Height);
                SaveAndCleanup(pck);
            }
        }

        [TestMethod]
        public void AutofitRow_ShouldNotCalculateNewRowHeightWhenWrapTextIsTrue()
        {
            var defaultWidth = 15d;
            using (var pck = OpenPackage("AutofitRows_DefaultWidth_WrapText_False.xlsx", true))
            {
                var sheet = pck.Workbook.Worksheets.Add("Sheet1");
                sheet.Cells["A1"].Value = "A long text that needs some serious autofit of row height";
                sheet.Cells["A1"].Style.WrapText = false;
                sheet.Cells["A1"].AutoFitRows();
                Assert.AreEqual(defaultWidth, sheet.Row(1).Height);
                SaveAndCleanup(pck);
            }
        }

        [TestMethod]
        public void AutofitRow_SetRowHeight_CustomWidth_Regular()
        {
            using (var pck = OpenPackage("AutofitRows_CustomWidth_WrapText_True_Regular.xlsx", true))
            {
                var sheet = pck.Workbook.Worksheets.Add("Sheet1");

                sheet.Cells["A1"].Value = "A long text that needs some serious autofit of row height";
                sheet.Cells["A1"].Style.WrapText = true;
                sheet.Cells["A1"].AutoFitRows();
                //Assert.AreEqual(118.3d, sheet.Row(1).Height);

                sheet.Column(2).Width = 25d;
                sheet.Cells["B2"].Value = "A long text that needs some serious autofit of row height";
                sheet.Cells["B2"].Style.WrapText = true;
                sheet.Cells["B2"].AutoFitRows();
                //Assert.AreEqual(30.9d, sheet.Row(2).Height);

                sheet.Column(3).Width = 20d;
                sheet.Cells["C3"].Value = "A long text that needs some serious autofit of row height";
                sheet.Cells["C3"].Style.WrapText = true;
                sheet.Cells["C3"].AutoFitRows();
                //Assert.AreEqual(45.5d, sheet.Row(3).Height);

                SaveAndCleanup(pck);
            }
        }

        [TestMethod]
        public void AutofitRow_SetRowHeight_CustomWidth_Regular_Linebreak()
        {
            using (var pck = OpenPackage("AutofitRows_CustomWidth_WrapText_True_Regular_Linebreak.xlsx", true))
            {
                var sheet = pck.Workbook.Worksheets.Add("Sheet1");

                sheet.Cells["A1"].Value = "A long text that needs some serious autofit of row height\n\r\n\rAnd some more text that needs some autofit";
                sheet.Cells["A1"].Style.WrapText = true;
                sheet.Cells["A1"].AutoFitRows();

                sheet.Column(2).Width = 25d;
                sheet.Cells["B2"].Value = "A long text that needs some serious autofit of row height\n\r\n\rAnd some more text that needs some autofit";
                sheet.Cells["B2"].Style.WrapText = true;
                sheet.Cells["B2"].AutoFitRows();

                sheet.Column(3).Width = 20d;
                sheet.Cells["C3"].Value = "A long text that needs some serious autofit of row height\n\nAnd some more text that needs some autofit";
                sheet.Cells["C3"].Style.WrapText = true;
                sheet.Cells["C3"].AutoFitRows();

                SaveAndCleanup(pck);
            }
        }

    }
}
