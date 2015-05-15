﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using NewLife.Reflection;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube
{
    /// <summary>Html扩展</summary>
    public static class HtmlExtensions
    {
        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEditor(this HtmlHelper Html, String name, Object value, Type type = null, String format = null, Object htmlAttributes = null)
        {
            if (type == null && value != null) type = value.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return Html.ForBoolean(name, value.ToBoolean());
                case TypeCode.DateTime:
                    return Html.ForDateTime(name, value.ToDateTime());
                case TypeCode.Decimal:
                    return Html.ForDecimal(name, Convert.ToDecimal(value));
                case TypeCode.Single:
                case TypeCode.Double:
                    return Html.ForDouble(name, value.ToDouble());
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Html.ForInt(name, Convert.ToInt64(value));
                case TypeCode.String:
                    return Html.ForString(name, value + "");
                default:
#if DEBUG
                    throw new Exception("不支持的类型" + type);
#else
                    return Html.Editor(name);
#endif
            }
        }

        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="expression"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEditor<TModel, TProperty>(this HtmlHelper<TModel> Html, Expression<Func<TModel, TProperty>> expression, Object htmlAttributes = null)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, Html.ViewData);
            var name = meta.PropertyName;
            var pi = typeof(TModel).GetProperty(name);

            return Html.ForEditor(name, Html.ViewData.Model.GetValue(pi), pi.PropertyType, null, htmlAttributes);
        }

        /// <summary>输出编辑框</summary>
        /// <param name="Html"></param>
        /// <param name="field"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static MvcHtmlString ForEditor(this HtmlHelper Html, FieldItem field, IEntity entity = null)
        {
            if (entity == null) entity = Html.ViewData.Model as IEntity;

            MvcHtmlString txt = null;
            if (field.ReadOnly)
            {
                var label = "<label class=\"control-label\">{0}</label>".F(entity[field.Name]);
                txt = new MvcHtmlString(label);
            }
            else if (field.Type == typeof(String) && (field.Length <= 0 || field.Length > 300))
            {
                txt = Html.ForString(field.Name, (String)entity[field.Name], field.Length);
            }
            else
            {
                // 如果是实体树，并且当前是父级字段，则生产下拉
                if (entity is IEntityTree)
                {
                    var fact = EntityFactory.CreateOperate(entity.GetType());
                    var set = entity.GetType().GetValue("Setting") as IEntityTreeSetting;
                    if (set != null && set.Parent == field.Name)
                    {
                        var root = entity.GetType().GetValue("Root") as IEntityTree;
                        // 找到完整菜单树，但是排除当前节点这个分支
                        var list = root.FindAllChildsExcept(entity as IEntityTree);
                        var data = new SelectList(list, set.Key, "TreeNodeText", entity[field.Name]);
                        return Html.DropDownList(field.Name, data);
                    }
                }
                // 如果有表间关系，且是当前字段
                if (field.Table.DataTable.Relations.Count > 0)
                {
                    var dr = field.Table.DataTable.Relations.FirstOrDefault(e => e.Column.EqualIgnoreCase(field.Name));
                    // 为该字段创建下拉菜单
                    if (dr != null)
                    {
                        var rt = EntityFactory.CreateOperate(dr.RelationTable);
                        var list = rt.FindAllWithCache();
                        var data = new SelectList(list, dr.RelationColumn, rt.Master.Name, entity[field.Name]);
                        return Html.DropDownList(field.Name, data, "无");
                    }
                }

                txt = Html.ForEditor(field.Name, entity[field.Name], field.Type);
            }

            //if (showDescription)
            //{
            //    var des = field.Description.TrimStart(field.DisplayName).TrimStart("。");
            //    if (!des.IsNullOrWhiteSpace())
            //    {
            //        des = "<p class=\"help-block\">{0}</p>".F(des);
            //        txt = new MvcHtmlString(txt.ToString() + des);
            //    }
            //}

            return txt;
        }

        #region 基础属性
        /// <summary>输出字符串</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForString(this HtmlHelper Html, String name, String value, Int32 length = 0, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            // 首先输出图标
            var ico = "";

            MvcHtmlString txt = null;
            if (name.EqualIgnoreCase("Pass", "Password"))
            {
                txt = Html.Password(name, (String)value, atts);
            }
            else if (name.EqualIgnoreCase("Phone"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-phone-alt\"></i></span>";
                if (!atts.ContainsKey("@type")) atts.Add("@type", "phone");
                txt = Html.TextBox(name, (String)value, atts);
            }
            else if (name.EqualIgnoreCase("email", "mail"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-envelope\"></i></span>";
                if (!atts.ContainsKey("@type")) atts.Add("@type", "email");
                txt = Html.TextBox(name, (String)value, atts);
            }
            else if (name.EndsWithIgnoreCase("url"))
            {
                ico = "<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-home\"></i></span>";
                if (!atts.ContainsKey("@type")) atts.Add("@type", "url");
                txt = Html.TextBox(name, (String)value, atts);
            }
            else if (length < 0 || length > 300)
            {
                txt = Html.TextArea(name, (String)value, atts);
            }
            else
            {
                txt = Html.TextBox(name, (String)value, atts);
            }

            return new MvcHtmlString(ico.ToString() + txt);
        }

        /// <summary>输出整数</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForInt(this HtmlHelper Html, String name, Int64 value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");
            if (!atts.ContainsKey("role")) atts.Add("role", "number");

            return Html.TextBox(name, value, format, atts);
        }

        /// <summary>时间日期输出</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDateTime(this HtmlHelper Html, String name, DateTime value, String format = null, Object htmlAttributes = null)
        {
            //var fullHtmlFieldName = Html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            //if (String.IsNullOrEmpty(fullHtmlFieldName))
            //    throw new ArgumentException("", "name");

            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("type")) atts.Add("type", "date");
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control date form_datetime");

            var obj = value.ToFullString();
            if (value <= DateTime.MinValue) obj = null;
            //if (format.IsNullOrWhiteSpace()) format = "yyyy-MM-dd HH:mm:ss";

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"fa fa-calendar\"></i></span>");

            var txt = Html.TextBox(name, obj, format, atts);
            //var txt = BuildInput(InputType.Text, name, obj, atts);

            return new MvcHtmlString(ico.ToString() + txt);
        }

        /// <summary>时间日期输出</summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="Html"></param>
        /// <param name="expression"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDateTime<TModel, TProperty>(this HtmlHelper<TModel> Html, Expression<Func<TModel, TProperty>> expression, String format = null, Object htmlAttributes = null)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, Html.ViewData);
            var entity = Html.ViewData.Model as IEntity;
            var value = (DateTime)entity[meta.PropertyName];

            return Html.ForDateTime(meta.PropertyName, value, format, htmlAttributes);
        }

        /// <summary>输出布尔型</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForBoolean(this HtmlHelper Html, String name, Boolean value, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            return Html.CheckBox(name, value, atts);
        }

        /// <summary>输出货币类型</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDecimal(this HtmlHelper Html, String name, Decimal value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-yen\"></i></span>");
            var txt = Html.TextBox(name, value, format, atts);

            return new MvcHtmlString(ico.ToString() + txt);
        }

        /// <summary>输出浮点数</summary>
        /// <param name="Html"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDouble(this HtmlHelper Html, String name, Double value, String format = null, Object htmlAttributes = null)
        {
            var atts = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (!atts.ContainsKey("class")) atts.Add("class", "form-control");

            // 首先输出图标
            var ico = Html.Raw("<span class=\"input-group-addon\"><i class=\"glyphicon glyphicon-yen\"></i></span>");
            var txt = Html.TextBox(name, value, format, atts);

            return new MvcHtmlString(ico.ToString() + txt);
        }
        #endregion

        #region 专有属性
        /// <summary>输出描述</summary>
        /// <param name="Html"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static MvcHtmlString ForDescription(this HtmlHelper Html, FieldItem field)
        {
            var des = field.Description.TrimStart(field.DisplayName).TrimStart("。");
            if (des.IsNullOrWhiteSpace()) return new MvcHtmlString(null);

            return new MvcHtmlString("<p class=\"help-block\">{0}</p>".F(des));
        }
        #endregion
    }
}