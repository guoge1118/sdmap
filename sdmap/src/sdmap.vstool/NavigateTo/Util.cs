﻿using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Diagnostics;
using EnvDTE;
using System.IO;

namespace sdmap.Vstool.NavigateTo
{
    internal static class Util
    {
        public static IEnumerable<ProjectItem> GetSolutionAllSdmapFiles(
            IServiceProvider serviceProvider)
        {
            IEnumerable<ProjectItem> GetAllProjectItems(ProjectItems projectItems)
            {
                foreach (var item in projectItems.OfType<ProjectItem>())
                {
                    if (item.ProjectItems.Count > 0)
                    {
                        foreach (var subItem in GetAllProjectItems(item.ProjectItems))
                        {
                            yield return subItem;
                        }
                    }
                    else
                    {
                        yield return item;
                    }
                }
            }

            var solution = (IVsSolution2)serviceProvider.GetService(typeof(IVsSolution));
            return GetCSharpProjects(solution)
                .AsParallel()
                .Select(x => x.ProjectItems)
                .SelectMany(x => GetAllProjectItems(x))
                .Where(x => x.FileCount == 1)
                .Where(x => x.FileNames[0].ToUpperInvariant().EndsWith(".SDMAP"));
        }

        public static IEnumerable<EnvDTE.Project> GetCSharpProjects(IVsSolution solution)
        {
            foreach (IVsHierarchy hier in GetProjectsInSolution(solution))
            {
                EnvDTE.Project project = GetDTEProject(hier);
                if (project != null && IsCSharpProjectGuid(project.Kind))
                    yield return project;
            }
        }

        private static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution)
        {
            return GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);
        }

        private static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
                yield break;

            IEnumHierarchies enumHierarchies;
            Guid guid = Guid.Empty;
            solution.GetProjectEnum((uint)flags, ref guid, out enumHierarchies);
            if (enumHierarchies == null)
                yield break;

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            uint fetched;
            while (enumHierarchies.Next(1, hierarchy, out fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                    yield return hierarchy[0];
            }
        }

        private static EnvDTE.Project GetDTEProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException("hierarchy");

            object obj;
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
            return obj as EnvDTE.Project;
        }

        private static bool IsCSharpProjectGuid(string guid)
        {
            return
                guid == VsConsts.CSharpProject ||
                guid == VsConsts.CSharpCoreProject;
        }
    }
}
