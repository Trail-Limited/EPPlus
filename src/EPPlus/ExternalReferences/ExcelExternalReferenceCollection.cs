﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  04/16/2021         EPPlus Software AB       EPPlus 5.7
 *************************************************************************************************/
using OfficeOpenXml;
using OfficeOpenXml.Core.CellStore;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using OfficeOpenXml.Packaging;
using OfficeOpenXml.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace OfficeOpenXml.ExternalReferences
{
    public class ExcelExternalReferenceCollection : IEnumerable<ExcelExternalLink>
    {
        List<ExcelExternalLink> _list=new List<ExcelExternalLink>();
        ExcelWorkbook _wb;
        internal ExcelExternalReferenceCollection(ExcelWorkbook wb)
        {
            _wb = wb;
            LoadExternalReferences();
        }
        internal void AddInternal(ExcelExternalLink externalReference)
        {
            _list.Add(externalReference);
        }
        public IEnumerator<ExcelExternalLink> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        public int Count { get { return _list.Count; } }
        public ExcelExternalLink this[int index]
        {
            get
            {
                return _list[index];
            }
        }
        internal void LoadExternalReferences()
        {
            XmlNodeList nl = _wb.WorkbookXml.SelectNodes("//d:externalReferences/d:externalReference", _wb.NameSpaceManager);
            if (nl != null)
            {
                foreach (XmlElement elem in nl)
                {
                    string rID = elem.GetAttribute("r:id");
                    var rel = _wb.Part.GetRelationship(rID);
                    var part = _wb._package.ZipPackage.GetPart(UriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri));
                    var xr = new XmlTextReader(part.GetStream());
                    while (xr.Read())
                    {
                        if (xr.NodeType == XmlNodeType.Element)
                        {
                            switch (xr.Name)
                            {
                                case "externalBook":
                                    AddInternal(new ExcelExternalWorkbook(_wb, xr, part, elem));
                                    break;
                                case "ddeLink":
                                    AddInternal(new ExcelExternalDdeLink(_wb, xr, part, elem));
                                    break;
                                case "oleLink":
                                    AddInternal(new ExcelExternalOleLink(_wb, xr, part, elem));
                                    break;
                                case "extLst":

                                    break; 
                                default:    
                                    break;
                            }
                        }
                    }
                    xr.Close();
                }
            }
        }
        /// <summary>
        /// Delete the external link at the zero-based index.
        /// </summary>
        /// <param name="index">The zero-based index</param>
        public void Delete(int index)
        {
            if(index < 0 || index>=_list.Count)
            {
                throw (new ArgumentOutOfRangeException(nameof(index)));
            }
            Delete(_list[index]);
        }
        /// <summary>
        /// Delete the specifik external link
        /// </summary>
        /// <param name="externalReference"></param>
        public void Delete(ExcelExternalLink externalReference)
        {
            var ix = _list.IndexOf(externalReference);
            
            _wb._package.ZipPackage.DeletePart(externalReference.Part.Uri);

            if(externalReference.ExternalLinkType==eExternalLinkType.ExternalWorkbook)
            {
                ExternalLinksHandler.BreakFormulaLinks(_wb, ix, true);
            }

            var extRefs = externalReference.WorkbookElement.ParentNode;
            extRefs?.RemoveChild(externalReference.WorkbookElement);
            if(extRefs?.ChildNodes.Count==0)
            {
                extRefs.ParentNode?.RemoveChild(extRefs);
            }
            _list.Remove(externalReference);
        }
        /// <summary>
        /// Clear all external links and break any formula links.
        /// </summary>
        public void Clear()
        {
            if (_list.Count == 0) return;
            var extRefs = _list[0].WorkbookElement.ParentNode;

            ExternalLinksHandler.BreakAllFormulaLinks(_wb);
            while (_list.Count>0)
            {
                _wb._package.ZipPackage.DeletePart(_list[0].Part.Uri);
                _list.RemoveAt(0);
            }

            extRefs?.ParentNode?.RemoveChild(extRefs);
        }
        /// <summary>
        /// A list of directories to look for the external files that can not be found on the path of the uri.
        /// </summary>
        public List<DirectoryInfo> Directories
        {
            get;
        } = new List<DirectoryInfo>();
        /// <summary>
        /// Will load all external workbooks that can be accessed via the file system.
        /// External workbook referenced via other protocols must be loaded manually.
        /// </summary>
        /// <returns>Returns false if any workbook fails to loaded otherwise true. </returns>
        public bool LoadWorkbooks()
        {
            bool ret = true;
            foreach (var link in _list)
            {
                if(link.ExternalLinkType==eExternalLinkType.ExternalWorkbook)
                {
                    var externalWb = link.As.ExternalWorkbook;
                    if(externalWb.Package==null)
                    {
                        if(externalWb.Load() == false)
                        {
                            ret = false;
                        }
                    }
                }
            }
            return ret;
        }
        internal int GetExternalReference(string extRef)
        {
            if (string.IsNullOrEmpty(extRef)) return -1;
            if(extRef.Any(c=>char.IsDigit(c)==false))
            {
                if(ExcelExternalLink.HasWebProtocol(extRef))
                {
                    for (int ix = 0; ix < _list.Count; ix++)
                    {
                        if (_list[ix].ExternalLinkType == eExternalLinkType.ExternalWorkbook)
                        {
                            if (extRef.Equals(_list[ix].As.ExternalWorkbook.ExternalReferenceUri.OriginalString, StringComparison.OrdinalIgnoreCase))
                            {
                                return ix;
                            }
                        }
                    }
                    return -1;
                }
                if (extRef.StartsWith("file:///")) extRef = extRef.Substring(8);
                var fi = new FileInfo(extRef);
                int ret=-1;
                for (int ix=0;ix<_list.Count;ix++)
                {
                    if (_list[ix].ExternalLinkType == eExternalLinkType.ExternalWorkbook)
                    {
                        
                        var fileName = _list[ix].As.ExternalWorkbook.ExternalReferenceUri.OriginalString;
                        if (ExcelExternalLink.HasWebProtocol(fileName))
                        {
                            if (fileName.Equals(extRef, StringComparison.OrdinalIgnoreCase))
                            {
                                return ix;
                            }
                            continue;
                        }
                        if (fileName.StartsWith("file:///")) fileName = fileName.Substring(8);
                        var erFile = new FileInfo(fileName);
                        if (fi.FullName == erFile.FullName)
                        {
                            return ix;
                        }
                        else if (fi.Name == erFile.Name)
                        {
                            ret = ix;
                        }
                    }
                }
                return ret;
            }
            else
            {
                var ix = int.Parse(extRef)-1;
                if(ix<_list.Count)
                {
                    return ix;
                }
            }
            return -1;
        }
        internal int GetIndex(ExcelExternalLink link)
        {
            return _list.IndexOf(link);
        }
    }
}