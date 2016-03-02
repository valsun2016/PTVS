// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PythonTools.Analysis;
using Microsoft.PythonTools.Intellisense;
using Microsoft.PythonTools.Language;
using Microsoft.PythonTools.Parsing;
using Microsoft.PythonTools.Project;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools.Navigation;
using Microsoft.VisualStudioTools.Project;

namespace Microsoft.PythonTools.Navigation {
    class PythonFileLibraryNode : LibraryNode {
        private readonly HierarchyNode _hierarchy;
        public PythonFileLibraryNode(LibraryNode parent, HierarchyNode hierarchy, string name, string filename)
            : base(parent, name, filename, LibraryNodeType.Package | LibraryNodeType.Classes, children : new PythonFileChildren((PythonFileNode)hierarchy)) {
                _hierarchy = hierarchy;
        }

        class PythonFileChildren : IList<LibraryNode> {
            private readonly PythonFileNode _hierarchy;
            private LibraryNode[] _children;

            public PythonFileChildren(PythonFileNode hierarchy) {
                _hierarchy = hierarchy;
                
            }

            public void EnsureChildren() {
                if (_children == null) {
                    var projEntry = _hierarchy.GetProjectEntry();
                    var members = projEntry.GetAllAvailableMembers(new SourceLocation(0, 1, 1), GetMemberOptions.ExcludeBuiltins);
                    List<LibraryNode> children = new List<LibraryNode>();
                    foreach (var member in members) {
                        
                        var node = new PythonLibraryNode(null, member.Name, _hierarchy.ProjectMgr, _hierarchy.ID, member.MemberType);
                        children.Add(node);
                    }
                    _children = children.ToArray();
                }
            }

            public LibraryNode this[int index] {
                get {
                    EnsureChildren();
                    return _children[index];
                }

                set {
                    throw new NotImplementedException();
                }
            }

            public int Count {
                get {
                    EnsureChildren();
                    return _children.Length;
                }
            }

            public bool IsReadOnly {
                get {
                    return true;
                }
            }

            public void Add(LibraryNode item) {
                throw new NotImplementedException();
            }

            public void Clear() {
                throw new NotImplementedException();
            }

            public bool Contains(LibraryNode item) {
                EnsureChildren();
                return _children.Contains(item);
            }

            public void CopyTo(LibraryNode[] array, int arrayIndex) {
                EnsureChildren();
                _children.CopyTo(array, arrayIndex);
            }

            public IEnumerator<LibraryNode> GetEnumerator() {
                EnsureChildren();
                return ((IEnumerable<LibraryNode>)_children).GetEnumerator();
            }

            public int IndexOf(LibraryNode item) {
                EnsureChildren();
                return Array.IndexOf(_children, item);
            }

            public void Insert(int index, LibraryNode item) {
                throw new NotImplementedException();
            }

            public bool Remove(LibraryNode item) {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index) {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return _children.GetEnumerator();
            }
        }

        public override VSTREEDISPLAYDATA DisplayData {
            get {
                var res = new VSTREEDISPLAYDATA();

                // Use the default Module icon for modules
                res.hImageList = IntPtr.Zero;
                res.Image = res.SelectedImage = 90;
                return res;
            }
        }

        public override string Name {
            get {
                if (DuplicatedByName) {
                    StringBuilder sb = new StringBuilder(_hierarchy.Caption);
                    sb.Append(" (");
                    sb.Append(_hierarchy.ProjectMgr.Caption);
                    sb.Append(", ");
                    PythonFileNode.GetPackageName(_hierarchy, sb);
                    sb.Append(')');

                    return sb.ToString();
                }
                return base.Name;
            }
        }

        public override uint CategoryField(LIB_CATEGORY category) {
            switch (category) {
                case LIB_CATEGORY.LC_NODETYPE:
                    return (uint)_LIBCAT_NODETYPE.LCNT_HIERARCHY;
            }
            return base.CategoryField(category);
        }

        public override IVsSimpleObjectList2 DoSearch(VSOBSEARCHCRITERIA2 criteria) {
            var node = _hierarchy as PythonFileNode;
            if(node != null) {
                var analysis = node.GetProjectEntry();

                if (analysis != null) {
                    string expr = criteria.szName.Substring(criteria.szName.LastIndexOf(':') + 1);
                    var exprAnalysis = VsProjectAnalyzer.AnalyzeExpression(
                        analysis,
                        criteria.szName.Substring(criteria.szName.LastIndexOf(':') + 1),
                        new Parsing.SourceLocation(0, 1, 1)
                    ).Result;
                    
                    return EditFilter.GetFindRefLocations(_hierarchy.ProjectMgr.Site, expr, exprAnalysis.References);
                }
            }

            return null;
        }
    }
}
