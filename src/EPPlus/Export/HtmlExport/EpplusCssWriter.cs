﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  11/17/2021         EPPlus Software AB       Added Html Export
 *************************************************************************************************/
using OfficeOpenXml.Core.CellStore;
using OfficeOpenXml.Drawing.Theme;
using OfficeOpenXml.Style.XmlAccess;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Globalization;
using System;
using OfficeOpenXml.Utils;
using System.Text;

namespace OfficeOpenXml.Export.HtmlExport
{
    internal partial class EpplusCssWriter : HtmlWriterBase
    {
        protected HtmlExportSettings _settings;
        protected CssExportSettings _cssSettings;
        protected CssExclude _cssExclude;
        ExcelRangeBase _range;
        ExcelTheme _theme;
        internal eFontExclude _fontExclude;
        internal eBorderExclude _borderExclude;
        internal EpplusCssWriter(StreamWriter writer, ExcelRangeBase range, HtmlExportSettings settings, CssExportSettings cssSettings, CssExclude cssExclude) : base(writer) 
        {
            _settings = settings;
            _cssSettings = cssSettings;
            _cssExclude = cssExclude;
            Init(range);
        }
        internal EpplusCssWriter(Stream stream, ExcelRangeBase range, HtmlExportSettings settings, CssExportSettings cssSettings, CssExclude cssExclude) : base(stream, settings.Encoding)
        {
            _settings = settings;
            _cssSettings = cssSettings;
            _cssExclude = cssExclude;
            Init(range);
        }
        private void Init(ExcelRangeBase range)
        {
            _range = range;

            if (_range.Worksheet.Workbook.ThemeManager.CurrentTheme == null)
            {
                _range.Worksheet.Workbook.ThemeManager.CreateDefaultTheme();
            }
            _theme = range.Worksheet.Workbook.ThemeManager.CurrentTheme;
            _borderExclude = _cssExclude.Border;
            _fontExclude = _cssExclude.Font;
        }
        internal void RenderAdditionalAndFontCss(string tableClass)
        {
            if (_cssSettings.IncludeSharedClasses == false) return;
            WriteClass($"table.{tableClass}{{", _settings.Minify);
            if (_cssSettings.IncludeNormalFont)
            {
                var ns = _range.Worksheet.Workbook.Styles.GetNormalStyle();
                if (ns != null)
                {
                    WriteCssItem($"font-family:{ns.Style.Font.Name};", _settings.Minify);
                    WriteCssItem($"font-size:{ns.Style.Font.Size.ToString("g", CultureInfo.InvariantCulture)}pt;", _settings.Minify);
                }
            }
            foreach (var item in _cssSettings.AdditionalCssElements)
            {
                WriteCssItem($"{item.Key}:{item.Value};", _settings.Minify);
            }
            WriteClassEnd(_settings.Minify);

            //Class for hidden rows.
            WriteClass($".{_settings.StyleClassPrefix}hidden {{", _settings.Minify);
            WriteCssItem($"display:none;", _settings.Minify);
            WriteClassEnd(_settings.Minify);

            WriteClass($".{_settings.StyleClassPrefix}al {{", _settings.Minify);
            WriteCssItem($"text-align:left;", _settings.Minify);
            WriteClassEnd(_settings.Minify);
            WriteClass($".{_settings.StyleClassPrefix}ar {{", _settings.Minify);
            WriteCssItem($"text-align:right;", _settings.Minify);
            WriteClassEnd(_settings.Minify);

            var ws = _range.Worksheet;
            WriteClass($".{_settings.StyleClassPrefix}dcw {{", _settings.Minify);
            WriteCssItem($"width:{ExcelColumn.ColumnWidthToPixels(Convert.ToDecimal(ws.DefaultColWidth), ws.Workbook.MaxFontWidth)}px;", _settings.Minify);
            WriteClassEnd(_settings.Minify);

            WriteClass($".{_settings.StyleClassPrefix}drh {{", _settings.Minify);
            WriteCssItem($"height:{(int)(ws.DefaultRowHeight / 0.75)}px;", _settings.Minify);
            WriteClassEnd(_settings.Minify);
        }

        internal void AddToCss(ExcelStyles styles, int styleId, string styleClassPrefix)
        {
            var xfs = styles.CellXfs[styleId];
            var ns = styles.GetNormalStyle();
            if (HasStyle(xfs))
            {
                if (IsAddedToCache(xfs, out int id)==false)
                {
                    WriteClass($".{styleClassPrefix}s{id}{{", _settings.Minify);
                    if (xfs.FillId > 0)
                    {
                        WriteFillStyles(xfs.Fill);
                    }
                    if (xfs.FontId > 0)
                    {
                        WriteFontStyles(xfs.Font, ns.Style.Font);
                    }
                    if (xfs.BorderId > 0)
                    {
                        WriteBorderStyles(xfs.Border);
                    }
                    WriteStyles(xfs);
                    WriteClassEnd(_settings.Minify);
                }
            }
        }

        private bool IsAddedToCache(ExcelXfs xfs, out int id)
        {
            var key = GetStyleKey(xfs);
            if (_styleCache.ContainsKey(key))
            {
                id = _styleCache[key];
                return true;
            }
            else
            {
                id = _styleCache.Count+1;
                _styleCache.Add(key, id);
                return false;
            }
        }

        private void WriteStyles(ExcelXfs xfs)
        {
            if (_cssExclude.WrapText == false)
            {
                if (xfs.WrapText)
                {
                    WriteCssItem("white-space: break-spaces;", _settings.Minify);
                }
                else
                {
                    WriteCssItem("white-space: nowrap;", _settings.Minify);
                }
            }

            if (xfs.HorizontalAlignment != ExcelHorizontalAlignment.General && _cssExclude.HorizontalAlignment == false)
            {
                var hAlign = GetHorizontalAlignment(xfs);
                WriteCssItem($"text-align:{hAlign};", _settings.Minify);
            }

            if (xfs.VerticalAlignment != ExcelVerticalAlignment.Bottom && _cssExclude.VerticalAlignment == false)
            {
                var vAlign = GetVerticalAlignment(xfs);
                WriteCssItem($"vertical-align:{vAlign};", _settings.Minify);
            }
            if(xfs.TextRotation!=0 && _cssExclude.TextRotation==false)
            {
                WriteCssItem($"transform: rotate({xfs.TextRotation}deg);", _settings.Minify);
            }

            if(xfs.Indent>0 && _cssExclude.Indent == false)
            {
                WriteCssItem($"padding-left:{xfs.Indent * _cssSettings.IndentValue}{_cssSettings.IndentUnit};", _settings.Minify);
            }
        }

        private void WriteBorderStyles(ExcelBorderXml b)
        {
            if (EnumUtil.HasNotFlag(_borderExclude, eBorderExclude.Top)) WriteBorderItem(b.Top, "top");
            if (EnumUtil.HasNotFlag(_borderExclude, eBorderExclude.Bottom)) WriteBorderItem(b.Bottom, "bottom");
            if (EnumUtil.HasNotFlag(_borderExclude, eBorderExclude.Left)) WriteBorderItem(b.Left, "left");
            if (EnumUtil.HasNotFlag(_borderExclude, eBorderExclude.Right)) WriteBorderItem(b.Right, "right");
            //TODO add Diagonal
            //WriteBorderItem(b.DiagonalDown, "right");
            //WriteBorderItem(b.DiagonalUp, "right");
        }

        private void WriteBorderItem(ExcelBorderItemXml bi, string suffix)
        {
            if (bi.Style != ExcelBorderStyle.None)
            {
                var sb = new StringBuilder();
                sb.Append(GetBorderItemLine(bi.Style, suffix));
                if (bi.Color!=null && bi.Color.Exists)
                {
                    sb.Append($" {GetColor(bi.Color)}");
                }
                sb.Append(";");

                WriteCssItem(sb.ToString(), _settings.Minify);
            }
        }

        private void WriteFontStyles(ExcelFontXml f, ExcelFont nf)
        {
            if(string.IsNullOrEmpty(f.Name)==false && EnumUtil.HasNotFlag(_fontExclude, eFontExclude.Name) && f.Name.Equals(nf.Name) == false)
            {
                WriteCssItem($"font-family:{f.Name};", _settings.Minify);
            }
            if(f.Size>0 && EnumUtil.HasNotFlag(_fontExclude, eFontExclude.Size) && f.Size!=nf.Size)
            {
                WriteCssItem($"font-size:{f.Size.ToString("g", CultureInfo.InvariantCulture)}pt;", _settings.Minify);
            }
            if (f.Color!=null && f.Color.Exists && EnumUtil.HasNotFlag(_fontExclude, eFontExclude.Color) && AreColorEqual(f.Color, nf.Color)==false)
            {
                WriteCssItem($"color:{GetColor(f.Color)};", _settings.Minify);
            }
            if (f.Bold && EnumUtil.HasNotFlag(_fontExclude, eFontExclude.Bold) && nf.Bold!=f.Bold)
            {
                WriteCssItem("font-weight:bolder;", _settings.Minify);
            }
            if (f.Italic && EnumUtil.HasNotFlag(_fontExclude, eFontExclude.Italic) && nf.Italic != f.Italic)
            {
                WriteCssItem("font-style:italic;", _settings.Minify);
            }
            if (f.Strike && EnumUtil.HasNotFlag(_fontExclude, eFontExclude.Strike) && nf.Strike != f.Strike)
            {
                WriteCssItem("text-decoration:line-through solid;", _settings.Minify);
            }
            if (f.UnderLineType != ExcelUnderLineType.None && EnumUtil.HasNotFlag(_fontExclude, eFontExclude.Underline) && f.UnderLineType!=nf.UnderLineType)
            {
                switch (f.UnderLineType)
                {
                    case ExcelUnderLineType.Double:
                    case ExcelUnderLineType.DoubleAccounting:
                        WriteCssItem("text-decoration:underline double;", _settings.Minify);
                        break;
                    default:
                        WriteCssItem("text-decoration:underline solid;", _settings.Minify);
                        break;
                }
            }            
        }

        private bool AreColorEqual(ExcelColorXml c1, ExcelColor c2)
        {
            if (c1.Tint != c2.Tint) return false;
            if(c1.Indexed>=0)
            {
                return c1.Indexed == c2.Indexed;
            }
            else if(string.IsNullOrEmpty(c1.Rgb)==false)
            {
                return c1.Rgb == c2.Rgb;
            }
            else if(c1.Theme!=null)
            {
                return c1.Theme == c2.Theme;
            }
            else
            {
                return c1.Auto == c2.Auto;
            }
        }

        private void WriteFillStyles(ExcelFillXml f)
        {
            if (_cssExclude.Fill) return;
            if (f is ExcelGradientFillXml gf && gf.Type!=ExcelFillGradientType.None)
            {
                WriteGradient(gf);
            }
            else
            {
                if (f.PatternType == ExcelFillStyle.Solid)
                {
                    WriteCssItem($"background-color:{GetColor(f.BackgroundColor)};", _settings.Minify);
                }
                else
                {
                    WriteCssItem($"{PatternFills.GetPatternSvg(f.PatternType, GetColor(f.BackgroundColor), GetColor(f.PatternColor))}", _settings.Minify);
                }
            }
        }

        private void WriteGradient(ExcelGradientFillXml gradient)
        {
            if (gradient.Type == ExcelFillGradientType.Linear)
            {
                _writer.Write($"background: linear-gradient({(gradient.Degree + 90) % 360}deg");
            }
            else
            {
                _writer.Write($"background:radial-gradient(ellipse {gradient.Right  * 100}% {gradient.Bottom  * 100}%");
            }

            _writer.Write($",{GetColor(gradient.GradientColor1)} 0%");
            _writer.Write($",{GetColor(gradient.GradientColor2)} 100%");

            _writer.Write(");");
        }
        private string GetColor(ExcelColorXml c)
        {
            Color ret;
            if (!string.IsNullOrEmpty(c.Rgb))
            {
                if (int.TryParse(c.Rgb, NumberStyles.HexNumber, null, out int hex))
                {
                    ret = Color.FromArgb(hex);
                }
                else
                {
                    ret = Color.Empty;
                }
            }
            else if (c.Theme.HasValue)
            {
                ret = ColorConverter.GetThemeColor(_theme, c.Theme.Value);
            }
            else if (c.Indexed >= 0)
            {
                ret = ExcelColor.GetIndexedColor(c.Indexed);
            }
            else
            {
                //Automatic, set to black.
                ret = Color.Black;
            }
            if (c.Tint != 0)
            {
                ret = ColorConverter.ApplyTint(ret, Convert.ToDouble(c.Tint));
            }
            return "#" + ret.ToArgb().ToString("x8").Substring(2);
        }
        public void FlushStream()
        {
            _writer.Flush();
        }
    }
}