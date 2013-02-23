using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Sitecore.MultiRootTreelist.Code
{
	// Exact copy of Sitecore's TreeList class except for a few
	// minor changes in the OnLoad function (and constructor)
	public class MultiRootTreeList : Control, IContentField
	{
		// Fields
		private string _itemID;
		private Listbox _listBox;
		private bool _readOnly;
		private string _source;

		public MultiRootTreeList()
		{
			this.Class = "scContentControl scContentControlTreelist";
			this.Background = "white";
			base.Activation = true;
			this.ReadOnly = false;

			DataSources = new Dictionary<int, string>();
		}

		protected void Add()
		{
			if (!this.Disabled)
			{
				string viewStateString = base.GetViewStateString("ID");
				TreeviewEx ex = this.FindControl(viewStateString + "_all") as TreeviewEx;
				Assert.IsNotNull(ex, typeof(DataTreeview));
				Listbox listbox = this.FindControl(viewStateString + "_selected") as Listbox;
				Assert.IsNotNull(listbox, typeof(Listbox));
				Item selectionItem = ex.GetSelectionItem();
				if (selectionItem == null)
				{
					SheerResponse.Alert("Select an item in the Content Tree.", new string[0]);
				}
				else if (!this.HasExcludeTemplateForSelection(selectionItem))
				{
					if (this.IsDeniedMultipleSelection(selectionItem, listbox))
					{
						SheerResponse.Alert("You cannot select the same item twice.", new string[0]);
					}
					else if (this.HasIncludeTemplateForSelection(selectionItem))
					{
						SheerResponse.Eval("scForm.browser.getControl('" + viewStateString + "_selected').selectedIndex=-1");
						ListItem control = new ListItem();
						control.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("L");
						Sitecore.Context.ClientPage.AddControl(listbox, control);
						control.Header = selectionItem.DisplayName;
						control.Value = control.ID + "|" + selectionItem.ID;
						SheerResponse.Refresh(listbox);
						SetModified();
					}
				}
			}
		}

		protected void Down()
		{
			if (!this.Disabled)
			{
				ListItem item;
				string viewStateString = base.GetViewStateString("ID");
				Listbox listbox = this.FindControl(viewStateString + "_selected") as Listbox;
				Assert.IsNotNull(listbox, typeof(Listbox));
				int num = -1;
				for (int i = listbox.Controls.Count - 1; i >= 0; i--)
				{
					item = listbox.Controls[i] as ListItem;
					Assert.IsNotNull(item, typeof(ListItem));
					if (!item.Selected)
					{
						num = i - 1;
						break;
					}
				}
				for (int j = num; j >= 0; j--)
				{
					item = listbox.Controls[j] as ListItem;
					Assert.IsNotNull(item, typeof(ListItem));
					if (item.Selected)
					{
						string str2 = string.Format("$('{0}_selected').style.position = ($('{0}_selected').style.position == 'relative' ? 'static' : 'relative')", viewStateString);
						SheerResponse.Eval("scForm.browser.swapNode(scForm.browser.getControl('" + item.ID + "'), scForm.browser.getControl('" + item.ID + "').nextSibling);" + str2);
						listbox.Controls.Remove(item);
						listbox.Controls.AddAt(j + 1, item);
					}
				}
				SetModified();
			}
		}

		private string FormTemplateFilterForDisplay()
		{
			if ((string.IsNullOrEmpty(this.IncludeTemplatesForDisplay) && string.IsNullOrEmpty(this.ExcludeTemplatesForDisplay)) && (string.IsNullOrEmpty(this.IncludeItemsForDisplay) && string.IsNullOrEmpty(this.ExcludeItemsForDisplay)))
			{
				return string.Empty;
			}
			string str = string.Empty;
			string str2 = ("," + this.IncludeTemplatesForDisplay + ",").ToLower();
			string str3 = ("," + this.ExcludeTemplatesForDisplay + ",").ToLower();
			string str4 = "," + this.IncludeItemsForDisplay + ",";
			string str5 = "," + this.ExcludeItemsForDisplay + ",";
			if (!string.IsNullOrEmpty(this.IncludeTemplatesForDisplay))
			{
				if (!string.IsNullOrEmpty(str))
				{
					str = str + " and ";
				}
				str = str + string.Format("(contains('{0}', ',' + @@templateid + ',') or contains('{0}', ',' + @@templatekey + ','))", str2);
			}
			if (!string.IsNullOrEmpty(this.ExcludeTemplatesForDisplay))
			{
				if (!string.IsNullOrEmpty(str))
				{
					str = str + " and ";
				}
				str = str + string.Format("not (contains('{0}', ',' + @@templateid + ',') or contains('{0}', ',' + @@templatekey + ','))", str3);
			}
			if (!string.IsNullOrEmpty(this.IncludeItemsForDisplay))
			{
				if (!string.IsNullOrEmpty(str))
				{
					str = str + " and ";
				}
				str = str + string.Format("(contains('{0}', ',' + @@id + ',') or contains('{0}', ',' + @@key + ','))", str4);
			}
			if (string.IsNullOrEmpty(this.ExcludeItemsForDisplay))
			{
				return str;
			}
			if (!string.IsNullOrEmpty(str))
			{
				str = str + " and ";
			}
			return (str + string.Format("not (contains('{0}', ',' + @@id + ',') or contains('{0}', ',' + @@key + ','))", str5));
		}

		public string GetValue()
		{
			ListString str = new ListString();
			string viewStateString = base.GetViewStateString("ID");
			Listbox listbox = this.FindControl(viewStateString + "_selected") as Listbox;
			Assert.IsNotNull(listbox, typeof(Listbox));
			foreach (ListItem item in listbox.Items)
			{
				string[] strArray = item.Value.Split(new char[] { '|' });
				if (strArray.Length > 1)
				{
					str.Add(strArray[1]);
				}
			}
			return str.ToString();
		}

		private bool HasExcludeTemplateForSelection(Item item)
		{
			return ((item == null) || HasItemTemplate(item, this.ExcludeTemplatesForSelection));
		}

		private bool HasIncludeTemplateForSelection(Item item)
		{
			Assert.ArgumentNotNull(item, "item");
			return ((this.IncludeTemplatesForSelection.Length == 0) || HasItemTemplate(item, this.IncludeTemplatesForSelection));
		}

		private static bool HasItemTemplate(Item item, string templateList)
		{
			Assert.ArgumentNotNull(templateList, "templateList");
			if (item == null)
			{
				return false;
			}
			if (templateList.Length == 0)
			{
				return false;
			}
			string[] strArray = templateList.Split(new char[] { ',' });
			ArrayList list = new ArrayList(strArray.Length);
			for (int i = 0; i < strArray.Length; i++)
			{
				list.Add(strArray[i].Trim().ToLower());
			}
			return list.Contains(item.TemplateName.Trim().ToLower());
		}

		private bool IsDeniedMultipleSelection(Item item, Listbox listbox)
		{
			Assert.ArgumentNotNull(listbox, "listbox");
			if (item == null)
			{
				return true;
			}
			if (!this.AllowMultipleSelection)
			{
				foreach (ListItem item2 in listbox.Controls)
				{
					string[] strArray = item2.Value.Split(new char[] { '|' });
					if ((strArray.Length >= 2) && (strArray[1] == item.ID.ToString()))
					{
						return true;
					}
				}
			}
			return false;
		}

		// This method is the only method that differs from Sitecore's
		// TreeList class
		protected override void OnLoad(EventArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			if (!Sitecore.Context.ClientPage.IsEvent)
			{
				Database contentDatabase = Sitecore.Context.ContentDatabase;
				if (!string.IsNullOrEmpty(this.DatabaseName))
				{
					contentDatabase = Factory.GetDatabase(this.DatabaseName);
				}

				this.SetProperties();
				GridPanel child = new GridPanel();
				this.Controls.Add(child);
				child.Columns = 4;
				this.GetControlAttributes();
				foreach (string str in base.Attributes.Keys)
				{
					child.Attributes.Add(str, base.Attributes[str]);
				}
				child.Style["margin"] = "0px 0px 4px 0px";
				base.SetViewStateString("ID", this.ID);
				Literal literal = new Literal("All");
				literal.Class = "scContentControlMultilistCaption";
				child.Controls.Add(literal);
				child.SetExtensibleProperty(literal, "Width", "50%");
				child.SetExtensibleProperty(literal, "Row.Height", "20px");
				LiteralControl control = new LiteralControl(Images.GetSpacer(0x18, 1));
				child.Controls.Add(control);
				child.SetExtensibleProperty(control, "Width", "24px");
				literal = new Literal("Selected");
				literal.Class = "scContentControlMultilistCaption";
				child.Controls.Add(literal);
				child.SetExtensibleProperty(literal, "Width", "50%");
				control = new LiteralControl(Images.GetSpacer(0x18, 1));
				child.Controls.Add(control);
				child.SetExtensibleProperty(control, "Width", "24px");
				Scrollbox scrollbox = new Scrollbox();
				scrollbox.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("S");
				child.Controls.Add(scrollbox);
				if (!UIUtil.IsIE())
				{
					scrollbox.Padding = "0px";
				}
				scrollbox.Style["border"] = "3px window-inset";
				child.SetExtensibleProperty(scrollbox, "rowspan", "2");
				child.SetExtensibleProperty(scrollbox, "VAlign", "top");
				// Instantiate the MultiTreeview rather than TreeviewEx
				MultiRootTreeview ex = new MultiRootTreeview();
				// Set the ParentItem of the treeview to the current item
				// This allows the treeview to have a 'context' of where 
				// we currently are in the content tree
				Item parentItem = contentDatabase.GetItem(ItemID);
				ex.ParentItem = parentItem;
				ex.ID = this.ID + "_all";
				if (!string.IsNullOrEmpty(AncestorTemplateDatasource))
				{
					Item parent = parentItem;
					while (parent != null && String.Compare(parent.TemplateID.ToString(), AncestorTemplateDatasource, StringComparison.OrdinalIgnoreCase) != 0)
					{
						parent = parent.Parent;
					}
					ex.MultiTrees.Add(2, parent);
				}
				else if (DataSources.Count > 0)
				{
					foreach (KeyValuePair<int, string> kvp in DataSources)
					{
						if (!string.IsNullOrEmpty(kvp.Value))
						{
							if (kvp.Value == ".")
							{
								ex.MultiTrees.Add(kvp.Key, parentItem);
							}
							else if (kvp.Value.StartsWith("."))
							{
								string path = kvp.Value.Replace(".", parentItem.Paths.FullPath);
								ex.MultiTrees.Add(kvp.Key, contentDatabase.GetItem(path));
							}
							else
							{
								ex.MultiTrees.Add(kvp.Key, contentDatabase.GetItem(kvp.Value));
							}
						}
					}
				}
				scrollbox.Controls.Add(ex);
				ex.DblClick = this.ID + ".Add";
				ex.AllowDragging = false;
				ImageBuilder builder = new ImageBuilder();
				builder.Src = "Applications/16x16/nav_right_blue.png";
				builder.ID = this.ID + "_right";
				builder.Width = 0x10;
				builder.Height = 0x10;
				builder.Margin = UIUtil.IsIE() ? "2px" : "2px 0px 2px 2px";
				builder.OnClick = Sitecore.Context.ClientPage.GetClientEvent(this.ID + ".Add");
				ImageBuilder builder2 = new ImageBuilder();
				builder2.Src = "Applications/16x16/nav_left_blue.png";
				builder2.ID = this.ID + "_left";
				builder2.Width = 0x10;
				builder2.Height = 0x10;
				builder2.Margin = UIUtil.IsIE() ? "2px" : "2px 0px 2px 2px";
				builder2.OnClick = Sitecore.Context.ClientPage.GetClientEvent(this.ID + ".Remove");
				LiteralControl control2 = new LiteralControl(builder + "<br/>" + builder2);
				child.Controls.Add(control2);
				child.SetExtensibleProperty(control2, "Width", "30");
				child.SetExtensibleProperty(control2, "Align", "center");
				child.SetExtensibleProperty(control2, "VAlign", "top");
				child.SetExtensibleProperty(control2, "rowspan", "2");
				Listbox listbox = new Listbox();
				child.Controls.Add(listbox);
				child.SetExtensibleProperty(listbox, "VAlign", "top");
				child.SetExtensibleProperty(listbox, "Height", "100%");
				this._listBox = listbox;
				listbox.ID = this.ID + "_selected";
				listbox.DblClick = this.ID + ".Remove";
				listbox.Style["width"] = "100%";
				listbox.Size = "10";
				listbox.Attributes["onchange"] = "javascript:document.getElementById('" + this.ID + "_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:''";
				listbox.Attributes["class"] = "scContentControlMultilistBox";
				ex.Enabled = !this.ReadOnly;
				listbox.Disabled = this.ReadOnly;
				ImageBuilder builder3 = new ImageBuilder();
				builder3.Src = "Applications/16x16/nav_up_blue.png";
				builder3.ID = this.ID + "_up";
				builder3.Width = 0x10;
				builder3.Height = 0x10;
				builder3.Margin = "2px";
				builder3.OnClick = Sitecore.Context.ClientPage.GetClientEvent(this.ID + ".Up");
				ImageBuilder builder4 = new ImageBuilder();
				builder4.Src = "Applications/16x16/nav_down_blue.png";
				builder4.ID = this.ID + "_down";
				builder4.Width = 0x10;
				builder4.Height = 0x10;
				builder4.Margin = "2px";
				builder4.OnClick = Sitecore.Context.ClientPage.GetClientEvent(this.ID + ".Down");
				control2 = new LiteralControl(builder3 + "<br/>" + builder4);
				child.Controls.Add(control2);
				child.SetExtensibleProperty(control2, "Width", "30");
				child.SetExtensibleProperty(control2, "Align", "center");
				child.SetExtensibleProperty(control2, "VAlign", "top");
				child.SetExtensibleProperty(control2, "rowspan", "2");
				child.Controls.Add(new LiteralControl("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"" + this.ID + "_help\"></div>"));
				DataContext context = new DataContext();
				child.Controls.Add(context);
				context.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("D");
				context.Filter = this.FormTemplateFilterForDisplay();
				ex.DataContext = context.ID;
				context.DataViewName = "Master";
				if (!string.IsNullOrEmpty(this.DatabaseName))
				{
					context.Parameters = "databasename=" + this.DatabaseName;
				}
				context.Root = this.DataSource;
				child.Fixed = true;
				ex.ShowRoot = true;
				child.SetExtensibleProperty(scrollbox, "Height", "100%");
				this.RestoreState();
			}
			base.OnLoad(args);
		}

		protected void Remove()
		{
			if (!this.Disabled)
			{
				string viewStateString = base.GetViewStateString("ID");
				Listbox listbox = this.FindControl(viewStateString + "_selected") as Listbox;
				Assert.IsNotNull(listbox, typeof(Listbox));
				SheerResponse.Eval("scForm.browser.getControl('" + viewStateString + "_all').selectedIndex=-1");
				SheerResponse.Eval("scForm.browser.getControl('" + viewStateString + "_help').innerHTML=''");
				foreach (ListItem item in listbox.Selected)
				{
					SheerResponse.Remove(item.ID);
					listbox.Controls.Remove(item);
				}
				SheerResponse.Refresh(listbox);
				SetModified();
			}
		}

		private void RestoreState()
		{
			string[] strArray = this.Value.Split(new char[] { '|' });
			if (strArray.Length > 0)
			{
				Database contentDatabase = Sitecore.Context.ContentDatabase;
				if (!string.IsNullOrEmpty(this.DatabaseName))
				{
					contentDatabase = Factory.GetDatabase(this.DatabaseName);
				}
				for (int i = 0; i < strArray.Length; i++)
				{
					string str = strArray[i];
					if (!string.IsNullOrEmpty(str))
					{
						ListItem child = new ListItem();
						child.ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("I");
						this._listBox.Controls.Add(child);
						child.Value = child.ID + "|" + str;
						Item item = contentDatabase.GetItem(str);
						if (item != null)
						{
							child.Header = item.DisplayName;
						}
						else
						{
							child.Header = str + ' ' + Translate.Text("[Item not found]");
						}
					}
				}
				SheerResponse.Refresh(this._listBox);
			}
		}

		protected static void SetModified()
		{
			Sitecore.Context.ClientPage.Modified = true;
		}

		private void SetProperties()
		{
			string id = StringUtil.GetString(new string[] { this.Source });
			if (Sitecore.Data.ID.IsID(id))
			{
				this.DataSource = this.Source;
			}
			else if ((this.Source != null) && !id.Trim().StartsWith("/", StringComparison.OrdinalIgnoreCase))
			{
				this.ExcludeTemplatesForSelection = StringUtil.ExtractParameter("ExcludeTemplatesForSelection", this.Source).Trim();
				this.IncludeTemplatesForSelection = StringUtil.ExtractParameter("IncludeTemplatesForSelection", this.Source).Trim();
				this.IncludeTemplatesForDisplay = StringUtil.ExtractParameter("IncludeTemplatesForDisplay", this.Source).Trim();
				this.ExcludeTemplatesForDisplay = StringUtil.ExtractParameter("ExcludeTemplatesForDisplay", this.Source).Trim();
				this.ExcludeItemsForDisplay = StringUtil.ExtractParameter("ExcludeItemsForDisplay", this.Source).Trim();
				this.IncludeItemsForDisplay = StringUtil.ExtractParameter("IncludeItemsForDisplay", this.Source).Trim();
				string strA = StringUtil.ExtractParameter("AllowMultipleSelection", this.Source).Trim().ToLower();
				this.AllowMultipleSelection = string.Compare(strA, "yes", StringComparison.OrdinalIgnoreCase) == 0;
				this.DataSource = StringUtil.ExtractParameter("DataSource", this.Source).Trim().ToLower();

				int dataSourceIndex = 2;
				string nthDS =
						StringUtil.ExtractParameter(String.Format("DataSource{0}", dataSourceIndex), Source).Trim().ToLower();
				while (!String.IsNullOrEmpty(nthDS))
				{
					DataSources.Add(dataSourceIndex, nthDS);
					nthDS = StringUtil.ExtractParameter(String.Format("DataSource{0}", ++dataSourceIndex), Source).Trim().ToLower();
				}

				this.DatabaseName = StringUtil.ExtractParameter("databasename", this.Source).Trim().ToLower();
				this.AncestorTemplateDatasource = StringUtil.ExtractParameter("AncestorTemplate", this.Source).Trim().ToLower();
			}
			else
			{
				this.DataSource = this.Source;
			}
		}

		public void SetValue(string text)
		{
			Assert.ArgumentNotNull(text, "text");
			this.Value = text;
		}

		protected void Up()
		{
			if (!this.Disabled)
			{
				string viewStateString = base.GetViewStateString("ID");
				Listbox listbox = this.FindControl(viewStateString + "_selected") as Listbox;
				Assert.IsNotNull(listbox, typeof(Listbox));
				ListItem selectedItem = listbox.SelectedItem;
				if (selectedItem != null)
				{
					int index = listbox.Controls.IndexOf(selectedItem);
					if (index != 0)
					{
						string str2 = string.Format("$('{0}_selected').style.position = ($('{0}_selected').style.position == 'relative' ? 'static' : 'relative')", viewStateString);
						SheerResponse.Eval("scForm.browser.swapNode(scForm.browser.getControl('" + selectedItem.ID + "'), scForm.browser.getControl('" + selectedItem.ID + "').previousSibling);" + str2);
						listbox.Controls.Remove(selectedItem);
						listbox.Controls.AddAt(index - 1, selectedItem);
						SetModified();
					}
				}
			}
		}

		public Dictionary<int, string> DataSources { get; private set; }

		private string _ancestorTemplateDatasource = string.Empty;
		public string AncestorTemplateDatasource
		{
			get
			{
				return _ancestorTemplateDatasource;
			}
			set
			{
				_ancestorTemplateDatasource = value;
			}
		}

		[Category("Data"), Description("If set to Yes, allows the same item to be selected more than once")]
		public bool AllowMultipleSelection
		{
			get
			{
				return base.GetViewStateBool("AllowMultipleSelection");
			}
			set
			{
				base.SetViewStateBool("AllowMultipleSelection", value);
			}
		}

		public string DatabaseName
		{
			get
			{
				return base.GetViewStateString("DatabaseName");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("DatabaseName", value);
			}
		}

		[Category("Data"), Description("Comma separated list of item names/ids.")]
		public string ExcludeItemsForDisplay
		{
			get
			{
				return base.GetViewStateString("ExcludeItemsForDisplay");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("ExcludeItemsForDisplay", value);
			}
		}

		[Category("Data"), Description("Comma separated list of template names. If this value is set, items based on these template will not be displayed in the tree.")]
		public string ExcludeTemplatesForDisplay
		{
			get
			{
				return base.GetViewStateString("ExcludeTemplatesForDisplay");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("ExcludeTemplatesForDisplay", value);
			}
		}

		[Category("Data"), Description("Comma separated list of template names. If this value is set, items based on these template will not be included in the menu.")]
		public string ExcludeTemplatesForSelection
		{
			get
			{
				return base.GetViewStateString("ExcludeTemplatesForSelection");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("ExcludeTemplatesForSelection", value);
			}
		}

		[Category("Data"), Description("Comma separated list of items names/ids.")]
		public string IncludeItemsForDisplay
		{
			get
			{
				return base.GetViewStateString("IncludeItemsForDisplay");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("IncludeItemsForDisplay", value);
			}
		}

		[Category("Data"), Description("Comma separated list of template names. If this value is set, only items based on these template can be displayed in the menu.")]
		public string IncludeTemplatesForDisplay
		{
			get
			{
				return base.GetViewStateString("IncludeTemplatesForDisplay");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("IncludeTemplatesForDisplay", value);
			}
		}

		[Category("Data"), Description("Comma separated list of template names. If this value is set, only items based on these template can be included in the menu.")]
		public string IncludeTemplatesForSelection
		{
			get
			{
				return base.GetViewStateString("IncludeTemplatesForSelection");
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				base.SetViewStateString("IncludeTemplatesForSelection", value);
			}
		}

		public string ItemID
		{
			get
			{
				return this._itemID;
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				this._itemID = value;
			}
		}

		public bool ReadOnly
		{
			get
			{
				return this._readOnly;
			}
			set
			{
				this._readOnly = value;
			}
		}

		public string Source
		{
			get
			{
				return this._source;
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				this._source = value;
			}
		}
	}
}
