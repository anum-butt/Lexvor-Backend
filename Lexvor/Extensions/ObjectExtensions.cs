using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Omu.ValueInjecter.Injections;

namespace Lexvor.Extensions
{
    public static class DateTimeExtensions {
        public static DateTime AddMonthsKeepDay(this DateTime date, int monthsToAdd) {
	        var newDate = date.AddMonths(monthsToAdd);

			// If this new date will be past the last day of the new month, make it the valid last day.
			var lastDay = newDate.AddMonths(1).AddDays(-1);

			return new DateTime(newDate.Year, newDate.Month, Math.Min(lastDay.Day, date.Day));
        }
        public static DateTime GetFirst(this DateTime date) {
	        return new DateTime(date.Year, date.Month, 1);
        }
        public static DateTime GetLast(this DateTime date) {
	        return date.GetFirst().AddMonths(1).AddDays(-1);
        }
        public static DateTime GetNextFirst(this DateTime date) {
	        var thisFirst = new DateTime(date.Year, date.Month, 1);
	        if (thisFirst.Date < DateTime.UtcNow.Date) {
				var nextFirst = new DateTime(thisFirst.AddMonths(1).Year, thisFirst.AddMonths(1).Month, 1);
				return nextFirst;
	        }

	        return thisFirst;
        }
    }

    public static class ObjectExtensions {
	    public static bool IsNull(this object obj) {
		    if (obj is string) {
			    return string.IsNullOrWhiteSpace(obj.ToString());
		    }
		    else {
			    return obj == null;
		    }
	    }
    }

    public static class StringExtensions {
	    public static string Truncate(this string value, int maxChars)
	    {
		    return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
	    }
    }

    public class NullInjection : LoopInjection {
        protected override bool MatchTypes(Type source, Type target) {
            return true;
        }

        protected override void SetValue(object source, object target, PropertyInfo sp, PropertyInfo tp) {
            var sourceVal = sp.GetValue(source);
            var targetVal = tp.GetValue(target);
            // If target value is null, always inject
            if (targetVal == null) {
                tp.SetValue(target, sourceVal);
            }
            // If target is not null and source is null, do not inject
            if ((targetVal != null && sourceVal == null) ||
                (targetVal is DateTime && (DateTime)targetVal != DateTime.MinValue && sourceVal is DateTime && (DateTime)sourceVal == DateTime.MinValue)) {

            }
            else {
                tp.SetValue(target, sourceVal);
            }
        }
    }

    /// <summary>
    /// Hierarchy node class which contains a nested collection of hierarchy nodes
    /// </summary>
    /// <typeparam name="T">Entity</typeparam>
    public class HierarchyNode<T> where T : class {
        public T Entity { get; set; }
        public IEnumerable<HierarchyNode<T>> ChildNodes { get; set; }
        public int Depth { get; set; }
        public T Parent { get; set; }
    }

    // Stefan Cruysberghs, July 2008, http://www.scip.be
    /// <summary>
    /// AsHierarchy extension methods for LINQ to Objects IEnumerable
    /// </summary>
    public static class LinqToObjectsExtensionMethods {
        private static IEnumerable<HierarchyNode<TEntity>>
          CreateHierarchy<TEntity, TProperty>(
            IEnumerable<TEntity> allItems,
            TEntity parentItem,
            Func<TEntity, TProperty> idProperty,
            Func<TEntity, TProperty> parentIdProperty,
            object rootItemId,
            int maxDepth,
            int depth) where TEntity : class {
            IEnumerable<TEntity> childs;

            if (rootItemId != null) {
                childs = allItems.Where(i => idProperty(i).Equals(rootItemId));
            } else {
                if (parentItem == null) {
                    childs = allItems.Where(i => parentIdProperty(i).Equals(default(TProperty)));
                } else {
                    childs = allItems.Where(i => parentIdProperty(i).Equals(idProperty(parentItem)));
                }
            }

            if (childs.Count() > 0) {
                depth++;

                if ((depth <= maxDepth) || (maxDepth == 0)) {
                    foreach (var item in childs)
                        yield return
                          new HierarchyNode<TEntity>() {
                              Entity = item,
                              ChildNodes =
                                CreateHierarchy(allItems.AsEnumerable(), item, idProperty, parentIdProperty, null, maxDepth, depth),
                              Depth = depth,
                              Parent = parentItem
                          };
                }
            }
        }

        /// <summary>
        /// LINQ to Objects (IEnumerable) AsHierachy() extension method
        /// </summary>
        /// <typeparam name="TEntity">Entity class</typeparam>
        /// <typeparam name="TProperty">Property of entity class</typeparam>
        /// <param name="allItems">Flat collection of entities</param>
        /// <param name="idProperty">Func delegete to Id/Key of entity</param>
        /// <param name="parentIdProperty">Func delegete to parent Id/Key</param>
        /// <returns>Hierarchical structure of entities</returns>
        public static IEnumerable<HierarchyNode<TEntity>> AsHierarchy<TEntity, TProperty>(
          this IEnumerable<TEntity> allItems,
          Func<TEntity, TProperty> idProperty,
          Func<TEntity, TProperty> parentIdProperty) where TEntity : class {
            return CreateHierarchy(allItems, default(TEntity), idProperty, parentIdProperty, null, 0, 0);
        }

        /// <summary>
        /// LINQ to Objects (IEnumerable) AsHierachy() extension method
        /// </summary>
        /// <typeparam name="TEntity">Entity class</typeparam>
        /// <typeparam name="TProperty">Property of entity class</typeparam>
        /// <param name="allItems">Flat collection of entities</param>
        /// <param name="idProperty">Func delegete to Id/Key of entity</param>
        /// <param name="parentIdProperty">Func delegete to parent Id/Key</param>
        /// <param name="rootItemId">Value of root item Id/Key</param>
        /// <returns>Hierarchical structure of entities</returns>
        public static IEnumerable<HierarchyNode<TEntity>> AsHierarchy<TEntity, TProperty>(
          this IEnumerable<TEntity> allItems,
          Func<TEntity, TProperty> idProperty,
          Func<TEntity, TProperty> parentIdProperty,
          object rootItemId) where TEntity : class {
            return CreateHierarchy(allItems, default(TEntity), idProperty, parentIdProperty, rootItemId, 0, 0);
        }

        /// <summary>
        /// LINQ to Objects (IEnumerable) AsHierachy() extension method
        /// </summary>
        /// <typeparam name="TEntity">Entity class</typeparam>
        /// <typeparam name="TProperty">Property of entity class</typeparam>
        /// <param name="allItems">Flat collection of entities</param>
        /// <param name="idProperty">Func delegete to Id/Key of entity</param>
        /// <param name="parentIdProperty">Func delegete to parent Id/Key</param>
        /// <param name="rootItemId">Value of root item Id/Key</param>
        /// <param name="maxDepth">Maximum depth of tree</param>
        /// <returns>Hierarchical structure of entities</returns>
        public static IEnumerable<HierarchyNode<TEntity>> AsHierarchy<TEntity, TProperty>(
          this IEnumerable<TEntity> allItems,
          Func<TEntity, TProperty> idProperty,
          Func<TEntity, TProperty> parentIdProperty,
          object rootItemId,
          int maxDepth) where TEntity : class {
            return CreateHierarchy(allItems, default(TEntity), idProperty, parentIdProperty, rootItemId, maxDepth, 0);
        }
    }
}
