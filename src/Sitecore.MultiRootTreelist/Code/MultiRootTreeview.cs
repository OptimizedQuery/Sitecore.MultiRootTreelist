using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Resources;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.HtmlControls.Data;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Sitecore.MultiRootTreelist.Code
{
	/*
	 * This is a Custom Treeview extends Sitecore's TreeviewEx class
	 * Only the Render(HtmlTextWriter output) Method has been rewritten.
	 * All other methods have been directly copied from TreeviewEx because
	 * They are private methods.
	 */
	public class MultiRootTreeview : TreeviewEx
	{
		private readonly List<Item> _updatedItems = new List<Item>();

		public Dictionary<int, Item> MultiTrees { get; private set; }

		public MultiRootTreeview()
		{
			MultiTrees = new Dictionary<int, Item>();
		}

		// This the only method that differs from TreeviewEx
		protected override void Render(HtmlTextWriter output)
		{
			Assert.ArgumentNotNull(output, "output");

			DataContext dataContext = GetDataContext();
			if (dataContext != null)
			{
				IDataView dataView = dataContext.DataView;
				if (dataView != null)
				{
					Item item2;
					Item item3;
					string filter = this.GetFilter();
					dataContext.GetState(out item2, out item3);
					// Based on the current Item, find the Widget Folder below it.
					foreach (KeyValuePair<int, Item> kvp in MultiTrees)
					{
						Item item = kvp.Value;

						if(item == null) continue;

						Render(output, dataView, filter, item, item);
					}
					// Use the Regular Data Context method to generate the static portion
					// of the tree defined in the template's source field
					this.Render(output, dataView, filter, item2, item3);
				}
			}
		}

		protected override void Render(HtmlTextWriter output, IDataView dataView, string filter, Item root, Item folder)
		{
			Assert.ArgumentNotNull(output, "output");
			Assert.ArgumentNotNull(dataView, "dataView");
			Assert.ArgumentNotNull(filter, "filter");
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(folder, "folder");
			output.Write("<div id=\"");
			output.Write(this.ID);
			output.Write("\" onclick=\"javascript:return Sitecore.Treeview.onTreeClick(this,event");
			if (!string.IsNullOrEmpty(this.Click))
			{
				output.Write(",'");
				output.Write(StringUtil.EscapeQuote(this.Click));
				output.Write("'");
			}
			output.Write(")\"");
			if (!string.IsNullOrEmpty(this.DblClick))
			{
				output.Write(" ondblclick=\"");
				output.Write(AjaxScriptManager.GetEventReference(this.DblClick));
				output.Write("\"");
			}
			if (!string.IsNullOrEmpty(this.ContextMenu))
			{
				output.Write(" oncontextmenu=\"");
				output.Write(AjaxScriptManager.GetEventReference(this.ContextMenu));
				output.Write("\"");
			}
			if (this.AllowDragging)
			{
				output.Write(" onmousedown=\"javascript:return Sitecore.Treeview.onTreeDrag(this,event)\" onmousemove=\"javascript:return Sitecore.Treeview.onTreeDrag(this,event)\" ondragstart=\"javascript:return Sitecore.Treeview.onTreeDrag(this,event)\" ondragover=\"javascript:return Sitecore.Treeview.onTreeDrop(this,event)\" ondrop=\"javascript:return Sitecore.Treeview.onTreeDrop(this,event)\"");
			}
			output.Write(">");
			output.Write("<input id=\"");
			output.Write(this.ID);
			output.Write("_Selected\" type=\"hidden\" value=\"" + folder.ID.ToShortID() + "\"/>");
			output.Write("<input id=\"");
			output.Write(this.ID);
			output.Write("_Database\" type=\"hidden\" value=\"" + folder.Database.Name + "\"/>");
			output.Write("<input id=\"");
			output.Write(this.ID);
			output.Write("_Parameters\" type=\"hidden\" value=\"" + this.GetParameters() + "\"/>");
			if (this.ShowRoot)
			{
				this.RenderNode(output, dataView, filter, root, root, folder);
			}
			else
			{
				foreach (Item item in dataView.GetChildren(root, string.Empty, true, 0, 0, this.GetFilter()))
				{
					this.RenderNode(output, dataView, filter, root, item, folder);
				}
			}
			output.Write("</div>");
		}

		private void RenderNode(HtmlTextWriter output, IDataView dataView, string filter, Item root, Item parent, Item folder)
		{
			Assert.ArgumentNotNull(output, "output");
			Assert.ArgumentNotNull(dataView, "dataView");
			Assert.ArgumentNotNull(filter, "filter");
			Assert.ArgumentNotNull(root, "root");
			Assert.ArgumentNotNull(parent, "parent");
			Assert.ArgumentNotNull(folder, "folder");
			bool isExpanded = (parent.ID == root.ID) || (parent.Axes.IsAncestorOf(folder) && (parent.ID != folder.ID));
			this.RenderNodeBegin(output, dataView, filter, parent, parent.ID == folder.ID, isExpanded);
			if (isExpanded)
			{
				ItemCollection items = dataView.GetChildren(parent, string.Empty, true, 0, 0, this.GetFilter());
				if (items != null)
				{
					foreach (Item item in items)
					{
						this.RenderNode(output, dataView, filter, root, item, folder);
					}
				}
			}
			RenderNodeEnd(output);
		}

		private void RenderParent(HtmlTextWriter output, Item parent)
		{
			Assert.ArgumentNotNull(output, "output");
			Assert.ArgumentNotNull(parent, "parent");
			IDataView dataView = this.GetDataView();
			if (dataView != null)
			{
				string filter = this.GetFilter();
				this.RenderNodeBegin(output, dataView, filter, parent, false, true);
				ItemCollection items = dataView.GetChildren(parent, string.Empty, true, 0, 0, filter);
				if (items != null)
				{
					foreach (Item item in items)
					{
						this.RenderNodeBegin(output, dataView, filter, item, false, false);
						RenderNodeEnd(output);
					}
				}
				RenderNodeEnd(output);
			}
		}

		protected override void RenderChildren(HtmlTextWriter output, Item parent)
		{
			Assert.ArgumentNotNull(output, "output");
			Assert.ArgumentNotNull(parent, "parent");
			IDataView dataView = this.GetDataView();
			if (dataView != null)
			{
				string filter = this.GetFilter();
				ItemCollection items = dataView.GetChildren(parent, string.Empty, true, 0, 0, filter);
				if (items != null)
				{
					foreach (Item item in items)
					{
						this.RenderNodeBegin(output, dataView, filter, item, false, false);
						RenderNodeEnd(output);
					}
				}
			}
		}

		protected override void RenderNodeBegin(HtmlTextWriter output, IDataView dataView, string filter, Item item, bool active, bool isExpanded)
		{
			Assert.ArgumentNotNull(output, "output");
			Assert.ArgumentNotNull(dataView, "dataView");
			Assert.ArgumentNotNull(filter, "filter");
			Assert.ArgumentNotNull(item, "item");
			string shortID = item.ID.ToShortID().ToString();
			string nodeID = this.GetNodeID(shortID);
			output.Write("<div id=\"");
			output.Write(nodeID);
			output.Write("\" class=\"scContentTreeNode\">");
			RenderTreeNodeGlyph(output, dataView, filter, item, shortID, isExpanded);
			string str3 = (active || this.SelectedIDs.Contains(shortID)) ? "scContentTreeNodeActive" : "scContentTreeNodeNormal";
			string style = GetStyle(item);
			output.Write("<a href=\"#\" class=\"" + str3 + "\"");
			if (!string.IsNullOrEmpty(item.Help.Text))
			{
				output.Write(" title=\"");
				output.Write(StringUtil.EscapeQuote(item.Help.Text));
				output.Write("\"");
			}
			output.Write(style);
			output.Write(">");
			RenderTreeNodeIcon(output, item);
			output.Write("<span hidefocus=\"true\" class=\"scContentTreeNodeTitle\" tabindex='0'>{0}</span>", this.GetHeaderValue(item));
			output.Write("</a>");
		}

		private static void RenderNodeEnd(HtmlTextWriter output)
		{
			Assert.ArgumentNotNull(output, "output");
			output.Write("</div>");
		}

		private static void RenderTreeNodeGlyph(HtmlTextWriter output, IDataView dataView, string filter, Item item, string id, bool isExpanded)
		{
			Assert.ArgumentNotNull(output, "output");
			Assert.ArgumentNotNull(dataView, "dataView");
			Assert.ArgumentNotNull(filter, "filter");
			Assert.ArgumentNotNull(item, "item");
			Assert.ArgumentNotNullOrEmpty(id, "id");
			ImageBuilder builder2 = new ImageBuilder();
			builder2.Class = "scContentTreeNodeGlyph";
			ImageBuilder builder = builder2;
			if (dataView.HasChildren(item, filter))
			{
				if (isExpanded)
				{
					builder.Src = "images/collapse15x15.gif";
				}
				else
				{
					builder.Src = "images/expand15x15.gif";
				}
			}
			else
			{
				builder.Src = "images/noexpand15x15.gif";
			}
			output.Write(builder.ToString());
		}

		private static void RenderTreeNodeIcon(HtmlTextWriter output, Item item)
		{
			Assert.ArgumentNotNull(output, "output");
			Assert.ArgumentNotNull(item, "item");
			ImageBuilder builder2 = new ImageBuilder();
			builder2.Src = item.Appearance.Icon;
			builder2.Width = 0x10;
			builder2.Height = 0x10;
			builder2.Class = "scContentTreeNodeIcon";
			ImageBuilder builder = builder2;
			if (!string.IsNullOrEmpty(item.Help.Text))
			{
				builder.Alt = item.Help.Text;
			}
			builder.Render(output);
		}

		private void AddUpdatedItem(Item item, bool updateParent)
		{
			if (item != null)
			{
				if (updateParent)
				{
					item = item.Parent;
					if (item == null)
					{
						return;
					}
				}
				foreach (Item item2 in this._updatedItems)
				{
					if (item2.Axes.IsAncestorOf(item))
					{
						return;
					}
				}
				for (int i = this._updatedItems.Count - 1; i >= 0; i--)
				{
					Item item3 = this._updatedItems[i];
					if (item.Axes.IsAncestorOf(item3))
					{
						this._updatedItems.Remove(item3);
					}
				}
				this._updatedItems.Add(item);
			}
		}

		private void DataContext_OnChanged(object sender)
		{
			DataContext dataContext = this.GetDataContext();
			if (dataContext != null)
			{
				this.UpdateFromDataContext(dataContext);
			}
		}

		protected override string GetNodeID(string shortID)
		{
			Assert.ArgumentNotNullOrEmpty(shortID, "shortID");
			return (this.ID + "_" + shortID);
		}

		private IDataView GetDataView()
		{
			string dataViewName = this.DataViewName;
			if (string.IsNullOrEmpty(dataViewName))
			{
				DataContext dataContext = this.GetDataContext();
				if (dataContext != null)
				{
					this.UpdateFromDataContext(dataContext);
				}
				dataViewName = this.DataViewName;
			}
			string parameters = this.Parameters;
			if (string.IsNullOrEmpty(dataViewName))
			{
				parameters = WebUtil.GetFormValue(this.ID + "_Parameters");
				UrlString str3 = new UrlString(parameters);
				dataViewName = str3["dv"];
			}
			return DataViewFactory.GetDataView(dataViewName, parameters);
		}

		private static string GetStyle(Item item)
		{
			Assert.ArgumentNotNull(item, "item");
			if (item.TemplateID == TemplateIDs.TemplateField)
			{
				return string.Empty;
			}
			string style = item.Appearance.Style;
			if (string.IsNullOrEmpty(style) && (item.Appearance.Hidden || item.RuntimeSettings.IsVirtual))
			{
				style = "color:#666666";
			}
			if (!string.IsNullOrEmpty(style))
			{
				style = " style=\"" + style + "\"";
			}
			return style;
		}

		protected override void UpdateFromDataContext(DataContext dataContext)
		{
			Assert.ArgumentNotNull(dataContext, "dataContext");
			string parameters = dataContext.Parameters;
			string filter = dataContext.Filter;
			string dataViewName = dataContext.DataViewName;
			if (((parameters != this.Parameters) || (filter != this.Filter)) || (dataViewName != this.DataViewName))
			{
				this.Parameters = parameters;
				this.Filter = filter;
				this.DataViewName = dataViewName;
				SheerResponse.SetAttribute(this.ID + "_Parameters", "value", this.GetParameters());
			}
		}

		private string GetParameters()
		{
			UrlString str = new UrlString(this.Parameters);
			str["dv"] = this.DataViewName;
			str["fi"] = this.Filter;
			return str.ToString();
		}

		private static string GetDragID(string id)
		{
			Assert.ArgumentNotNull(id, "id");
			int num = id.LastIndexOf("_");
			if (num >= 0)
			{
				id = StringUtil.Mid(id, num + 1);
			}
			if (ShortID.IsShortID(id))
			{
				id = ShortID.Decode(id);
			}
			return id;
		}

		private List<string> GetSelectedIDs()
		{
			return new List<string>(WebUtil.GetFormValue(this.ID + "_Selected").Split(new char[] { ',' }));
		}

		protected override DataContext GetDataContext()
		{
			return (Sitecore.Context.ClientPage.FindSubControl(this.DataContext) as DataContext);
		}

		protected override string GetFilter()
		{
			string filter = this.Filter;
			if (string.IsNullOrEmpty(filter))
			{
				UrlString str2 = new UrlString(WebUtil.GetFormValue(this.ID + "_Parameters"));
				filter = HttpUtility.UrlDecode(StringUtil.GetString(new string[] { str2["fi"] }));
			}
			return filter;
		}

		private void ItemMovedNotification(object sender, ItemMovedEventArgs args)
		{
			Assert.ArgumentNotNull(sender, "sender");
			Assert.ArgumentNotNull(args, "args");
			this.AddUpdatedItem(args.Item, true);
			Item item = args.Item.Database.GetItem(args.OldParentID);
			if (item != null)
			{
				this.AddUpdatedItem(item, false);
			}
		}

		private string Filter
		{
			get
			{
				return StringUtil.GetString(this.ViewState["Filter"]);
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				this.ViewState["Filter"] = value;
			}
		}

		private string DataViewName
		{
			get
			{
				return StringUtil.GetString(this.ViewState["DataViewName"]);
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				this.ViewState["DataViewName"] = value;
			}
		}

		private string Parameters
		{
			get
			{
				return StringUtil.GetString(this.ViewState["Parameters"]);
			}
			set
			{
				Assert.ArgumentNotNull(value, "value");
				this.ViewState["Parameters"] = value;
			}
		}
	}
}
