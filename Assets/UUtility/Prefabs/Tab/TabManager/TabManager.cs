using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using TMPro;
using UTool.Utility;

namespace UTool.TabSystem
{
    public class TabManager : MonoBehaviour 
    {
        public static TabManager _instance;

        public static void RefreshTabFeildAttributes()
        {
            if (_instance && _instance.setupDone)
                _instance.RefreshFeildAttributes();
        }

        [SerializeField] private Tab tabPrefab;
        [SpaceArea]
        [SerializeField] private UT ut;
        [SpaceArea]
        [SerializeField][Disable] private TabButton activeTab;
        [SpaceArea]
        [SerializeField] private TextMeshProUGUI versionDisplay;
        [SpaceArea]
        [SerializeField] private TabButton tabButtonPrefab;
        [SerializeField] private TabContent tabContentPrefab;
        [SpaceArea]
        [SerializeField] private Transform tabButtonHolder;
        [SerializeField] private Transform tabContentHolder;
        [SpaceArea]
        [SerializeField][ReorderableList(Foldable = true)] private List<Tab> tabs = new List<Tab>();

        private bool setupDone = false;
        private Dictionary<string, Tab> instanceTabs = new Dictionary<string, Tab>();
        private Dictionary<string, List<string>> tabNameInstances = new Dictionary<string, List<string>>();

        public void EarlyAwake()
        {
            _instance = this;
            SetupTabs();
        }

        public void MidAwake()
        {
            PopulateTabs();
        }

        public void LateAwake()
        {
        }

        public void ReloadManager()
        {
            foreach (Tab tab in tabs)
            {
                if (tab.controlledByAttribute)
                    DestroyImmediate(tab.gameObject);
                else
                {
                    List<TVariable> tVarToBeCleaned = new List<TVariable>();
                    tab.tabVariables.ForEach(tVar =>
                    {
                        if (tVar.controlledByAttribute)
                            tVarToBeCleaned.Add(tVar);
                    });

                    tVarToBeCleaned.ForEach(tVar =>
                    {
                        tab.tabVariables.Remove(tVar);
                    });
                }
            }

            instanceTabs.Clear();
            tabNameInstances.Clear();
            tabButtonHolder.DestroyAllChildImmediate();
            tabContentHolder.DestroyAllChildImmediate();

            SetupTabs();
            PopulateTabs();
        }

        public void SetupTabs()
        {
            setupDone = false;

            tabs.Clear();

            int childCount = transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Tab tab = transform.GetChild(i).GetComponent<Tab>();
                tabs.Add(tab);
            }

            RefreshFeildAttributes();

            foreach (Tab tab in tabs)
                tab.SetupTab();

            setupDone = true;
        }

        private string GenerateInstanceKey(object instance, string className)
        {
            int instanceId = instance.GetHashCode();
            return $"{className}_{instanceId}";
        }

        private void RefreshFeildAttributes()
        {
            tabNameInstances.Clear();
            List<HasTabFieldAttribute> hasTabFieldAttributes = GetNewTabFieldClassAttributes();
            BindAttributes(hasTabFieldAttributes);
        }

        private List<HasTabFieldAttribute> GetNewTabFieldClassAttributes()
        {
            List<Type> tabFieldClassTypes = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (Type type in assembly.GetTypes())
                {
                    HasTabFieldAttribute attr = type.GetCustomAttribute<HasTabFieldAttribute>(false);
                    if (attr != null && !tabFieldClassTypes.Contains(type))
                    {
                        tabFieldClassTypes.Add(type);
                    }
                }

            List<HasTabFieldAttribute> newAtt = new List<HasTabFieldAttribute>();
            foreach (Type currentType in tabFieldClassTypes)
            {
                object[] tabFieldClasses = GameObject.FindObjectsOfType(currentType);
                foreach (object tabFieldClass in tabFieldClasses)
                {
                    HasTabFieldAttribute attr = tabFieldClass.GetType().GetCustomAttribute<HasTabFieldAttribute>(false);
                    if (attr.active)
                        continue;

                    attr.parent = tabFieldClass;
                    attr.parentType = tabFieldClass.GetType();
                    newAtt.Add(attr);
                }
            }

            return newAtt;
        }

        private void BindAttributes(List<HasTabFieldAttribute> hasTabFieldAttributes)
        {
            // First pass: collect class instances
            foreach (HasTabFieldAttribute hasTabFieldAtt in hasTabFieldAttributes)
            {
                object parent = hasTabFieldAtt.parent;
                Type parentType = hasTabFieldAtt.parentType;
                string tabName = parentType.ToString().Split('.').Last();
                
                // Track this instance for the tab name
                Component component = parent as Component;
                string instanceName = component != null ? component.gameObject.name : parent.GetHashCode().ToString();
                
                if (!tabNameInstances.ContainsKey(tabName))
                {
                    tabNameInstances[tabName] = new List<string>();
                }
                tabNameInstances[tabName].Add(instanceName);
            }
            
            // Second pass: create tabs and bind attributes
            foreach (HasTabFieldAttribute hasTabFieldAtt in hasTabFieldAttributes)
            {
                hasTabFieldAtt.active = true;

                object parent = hasTabFieldAtt.parent;
                Type parentType = hasTabFieldAtt.parentType;

                string tabName = parentType.ToString().Split('.').Last();
                string instanceKey = GenerateInstanceKey(parent, tabName);
                
                // Get the GameObject name for this instance
                Component component = parent as Component;
                string instanceName = component != null ? component.gameObject.name : parent.GetHashCode().ToString();

                foreach (FieldInfo fieldInfo in parent.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    TabFieldAttribute attr = fieldInfo.GetCustomAttribute<TabFieldAttribute>(false);

                    if (attr == null)
                        continue;

                    attr.Setup(fieldInfo, parent);

                    Tab tab = GetAttributeTab(instanceKey, tabName, parent, instanceName);
                    string variableName = attr.variableName;

                    if (CheckForDuplicateBinding(tab, variableName))
                        continue;

                    TVariable tVariable = CreateAttributeControlledTVariable();

                    tVariable.attVariableName = variableName;
                    tVariable.variableType = attr.variableType;
                    tVariable.StoreDefaultValue(attr.defaultValue);
                    tVariable.tabFieldAttribute = attr;

                    tab.tabVariables.Add(tVariable);
                    attr.tVariable = tVariable;
                }

                foreach (MethodInfo methodInfo in parent.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    TabButtonAttribute attr = methodInfo.GetCustomAttribute<TabButtonAttribute>(false);

                    if (attr == null)
                        continue;

                    attr.Setup(methodInfo, parent);

                    Tab tab = GetAttributeTab(instanceKey, tabName, parent, instanceName);

                    if (CheckForDuplicateBinding(tab, attr.variableName))
                        continue;

                    TVariable tVariable = CreateAttributeControlledTVariable();

                    tVariable.attVariableName = attr.variableName;
                    tVariable.variableType = attr.variableType;
                    tVariable.tabButtonAttribute = attr;

                    tab.tabVariables.Add(tVariable);
                    attr.tVariable = tVariable;
                }
            }
        }

        private Tab GetAttributeTab(string instanceKey, string tabName, object instance, string instanceName)
        {
            if (instanceTabs.TryGetValue(instanceKey, out Tab tab))
                return tab;

            tab = CreateAttributeTab(tabName, instance, instanceName);
            instanceTabs[instanceKey] = tab;
            tabs.Add(tab);
            return tab;
        }

        private Tab CreateAttributeTab(string tabName, object instance, string instanceName)
        {
            Tab attTab = Instantiate(tabPrefab, transform);
            
            // Check if there are multiple instances of this class type
            bool hasMultipleInstances = tabNameInstances.ContainsKey(tabName) && tabNameInstances[tabName].Count > 1;
            
            if (hasMultipleInstances)
            {
                // Multiple instances: "GameObjectName (ClassName)"
                attTab.attTabName = $"{instanceName} ({tabName})";
            }
            else
            {
                // Single instance: just "ClassName"
                attTab.attTabName = tabName;
            }
            
            attTab.controlledByAttribute = true;
            attTab.usedByAttribute = true;
            return attTab;
        }

        private bool CheckForDuplicateBinding(Tab tab, string variableName)
        {
            TVariable tVariable = tab.tabVariables.Find(x => x.variableName == variableName);
            if (tVariable != null)
            {
                Debug.LogWarning($"TabManager - Binding Attribute Warning - Duplicate Not Fully Supported TVariables : '{tVariable.variableName}'");
                return true;
            }
            return false;
        }

        private TVariable CreateAttributeControlledTVariable()
        {
            TVariable tVariable = new TVariable();
            tVariable.controlledByAttribute = true;
            tVariable.usedByAttribute = true;
            return tVariable;
        }

        public void SelectTab(TabButton tab)
        {
            if (activeTab != null)
                activeTab.TabActive(false);

            tab.TabActive(true);
            activeTab = tab;
        }

        public Tab GetTab(object instance)
        {
            if (instance == null)
            {
                Debug.LogError("TabManager.GetTabForInstance: Instance cannot be null");
                return null;
            }
    
            // Get the type name without namespace
            string tabName = instance.GetType().ToString().Split('.').Last();
    
            // Generate the instance key used in our dictionary
            string instanceKey = GenerateInstanceKey(instance, tabName);
    
            // Try to find the tab for this instance
            if (instanceTabs.TryGetValue(instanceKey, out Tab tab))
            {
                return tab;
            }
    
            Debug.LogWarning($"TabManager: No tab found for {tabName} instance");
            return null;
        }

        private TabButton CreateTabButton()
        {
            TabButton button = Instantiate(tabButtonPrefab, tabButtonHolder);
            return button;
        }

        private TabContent CreateTabContent()
        {
            TabContent content = Instantiate(tabContentPrefab, tabContentHolder);
            content.gameObject.SetActive(false);
            return content;
        }

        private void Start()
        {
            versionDisplay.text = $"v{ut.version}";
        }

        private void PopulateTabs()
        {
            bool startSetup = false;
            foreach (Tab tab in tabs)
            {
                TabContent content = CreateTabContent();
                content.BindToTab(tab);
                content.AddTVariableControllers();

                TabButton button = CreateTabButton();
                button.Bind(this, tab);

                if (!startSetup)
                {
                    startSetup = true;
                    SelectTab(button);
                }
            }
        }
    }
}