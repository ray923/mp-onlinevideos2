﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OnlineVideos.Sites;
using System.IO;
using System.Reflection;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using OnlineVideos.Sites.WebAutomation.BrowserHost.Helpers;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost.Factories
{
    /// <summary>
    /// Static factory pattern
    /// </summary>
    public static class BrowserInstanceConnectorFactory
    {
        /// <summary>
        /// Load the first matching class from the site util dlls with the class name matching the connectorType
        /// </summary>
        /// <param name="connectorType"></param>
        /// <param name="logger"></param>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static BrowserUtilConnector GetConnector(string connectorType, ILog logger, WebBrowser browser = null)
        {
            var path = OnlineVideoSettings.Instance.DllsDir;
            var assemblies = new List<Assembly>();

            if (string.IsNullOrEmpty(path))
                path = Directory.GetCurrentDirectory() +"\\plugins\\windows\\onlinevideos";

            if (!Directory.Exists(path))
                path = Directory.GetCurrentDirectory();

            logger.Info(string.Format("Looking in {0} for connector dlls", path));

            if (Directory.Exists(path))
            {
                var dllFilesToCheck = Directory.GetFiles(path, "OnlineVideos.Sites.*.dll");

                if (dllFilesToCheck.Length == 0)
                {
                    path = Directory.GetCurrentDirectory();
                    logger.Info(string.Format("Looking in {0} for connector dlls", path));
                }

                dllFilesToCheck = Directory.GetFiles(path, "OnlineVideos.Sites.*.dll");

                foreach (string aDll in dllFilesToCheck)
                {
                    try
                    {
                        assemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(aDll)));
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                }
            }

            // Find the first type within the assemblies which matches the connectorType
            foreach (var assembly in assemblies)
            {
                WebBrowserPlayerCallbackService.LogInfo(string.Format("Looking for BrowserUtilConnector in {0} (Version: {1})",
                    assembly.GetName().Name,
                    assembly.GetName().Version.ToString()));
                try
                {
                    Type[] typeArray = assembly.GetExportedTypes();
                    foreach (Type type in typeArray)
                    {
                        if (type.BaseType != null && type.IsSubclassOf(typeof(BrowserUtilConnector)) && !type.IsAbstract)
                        {
                            if (type.FullName == connectorType)
                            {
                                // Weve hit gold!
                                var connector = Activator.CreateInstance(type) as BrowserUtilConnector;
                                if (connector != null)
                                {
                                    connector.Initialise(browser ?? new WebBrowser { ScriptErrorsSuppressed = true }, logger);
                                    return connector;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            return null;
        }


    }
}
