// -----------------------------------------------------------------------
// <copyright file="ChangeLogGenerator.cs" company="Lensgrinder, Ltd.">
//   Copyright (c) Lensgrinder Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
namespace Infrastructure.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;

    /// <summary>
    /// Static class for generating change logs.
    /// </summary>
    public static class ChangeLogGenerator
    {
        /// <summary>
        /// Change log inserted value.
        /// </summary>
        private const int Inserted = 0;

        /// <summary>
        /// Change log updated value.
        /// </summary>
        private const int Updated = 1;

        /// <summary>
        /// Change log added value.
        /// </summary>
        private const int Added = 2;

        /// <summary>
        /// Change log removed value.
        /// </summary>
        private const int Removed = 3;

        /// <summary>
        /// Generate change logs for the current context.
        /// </summary>
        /// <typeparam name="T">The change log type.</typeparam>
        /// <param name="context">The proxy context.</param>
        /// <param name="user">The user name.</param>
        /// <param name="changeLogEntitySetName">The name of the change log entity set.</param>
        public static void Generate<T>(DbContext context, IUser user, string changeLogEntitySetName)
            where T : IChangeLog, new()
        {
            context.ChangeTracker.DetectChanges();
            IObjectContextAdapter adapter = context as IObjectContextAdapter;
            Generate<T>(adapter.ObjectContext, user, changeLogEntitySetName);
        }

        /// <summary>
        /// Generate change logs for the current context.
        /// </summary>
        /// <typeparam name="T">The change log type.</typeparam>
        /// <param name="context">The proxy context.</param>
        /// <param name="user">The user name.</param>
        /// <param name="changeLogEntitySetName">The name of the change log entity set.</param>
        public static void Generate<T>(ObjectContext context, IUser user, string changeLogEntitySetName)
            where T : IChangeLog, new()
        {
            DateTimeOffset modifiedTime = DateTimeOffset.UtcNow;

            IEnumerable<ObjectStateEntry> deleted = context.ObjectStateManager.GetObjectStateEntries(EntityState.Deleted);
            IEnumerable<ObjectStateEntry> added = context.ObjectStateManager.GetObjectStateEntries(EntityState.Added);
            IEnumerable<ObjectStateEntry> changed = context.ObjectStateManager.GetObjectStateEntries(EntityState.Modified);

            // Handle deleted entities.
            foreach (ObjectStateEntry entry in deleted)
            {
                if (entry.IsRelationship == true)
                {
                    MakeChangeLogForRelationship<T>(context, user, changeLogEntitySetName, entry, modifiedTime);
                    continue;
                }
            }

            // Handle Added entities.
            foreach (ObjectStateEntry entry in added)
            {
                if (entry.IsRelationship == true)
                {
                    MakeChangeLogForRelationship<T>(context, user, changeLogEntitySetName, entry, modifiedTime);
                }

                if (entry.EntityKey == null) 
                { 
                    continue; 
                }

                IBaseEntity o = context.GetObjectByKey(entry.EntityKey) as IBaseEntity;
                IChangeLog c = context.GetObjectByKey(entry.EntityKey) as IChangeLog;
                if (o != null && c == null)
                {
                    o.InsertedTime = modifiedTime;
                    o.UpdatedTime = modifiedTime;
                    List<IChangeLog> changeLogs = MakeChangeLogForAddedEntry<T>(context, user, entry, o, modifiedTime);
                    foreach (IChangeLog log in changeLogs)
                    {
                        context.AddObject(changeLogEntitySetName, log);
                    }
                }
            }

            // Handle Each changed entities.
            foreach (ObjectStateEntry entry in changed)
            {
                if (entry.EntityKey == null) 
                { 
                    continue; 
                }

                IBaseEntity o = context.GetObjectByKey(entry.EntityKey) as IBaseEntity;
                IChangeLog c = context.GetObjectByKey(entry.EntityKey) as IChangeLog;
                if (o != null)
                {
                    o.UpdatedTime = modifiedTime;
                    List<IChangeLog> changeLogs = MakeChangeLogForModifiedEntry<T>(context, user, entry, o, modifiedTime);
                    foreach (IChangeLog log in changeLogs)
                    {
                        context.AddObject(changeLogEntitySetName, log);
                    }

                    if (changeLogs.Count == 0)
                    {
                        entry.ChangeState(EntityState.Unchanged);
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Make a change for a relationship.
        /// </summary>
        /// <typeparam name="T">The change log type.</typeparam>
        /// <param name="context">The object context.</param>
        /// <param name="user">The user.</param>
        /// <param name="changeLogEntitySetName">The change log entity set name.</param>
        /// <param name="entry">The changing entry.</param>
        /// <param name="modifiedTime">The change time.</param>
        private static void MakeChangeLogForRelationship<T>(
            ObjectContext context,
            IUser user,
            string changeLogEntitySetName,
            ObjectStateEntry entry, 
            DateTimeOffset modifiedTime)
            where T : IChangeLog, new()
        {
            if (IsChangeLogRelationship(entry, typeof(T)) == true)
            {
                return;
            }

            if (entry.State == EntityState.Deleted)
            {
                MakeChangeLogForDeletedRelationship<T>(context, user, changeLogEntitySetName, entry, modifiedTime);
            }
            else
            {
                MakeChangeLogForAddedRelationship<T>(context, user, changeLogEntitySetName, entry, modifiedTime);
            }

            return;
        }

        /// <summary>
        /// Make a change log for an added relationship.
        /// </summary>
        /// <typeparam name="T">The change log type.</typeparam>
        /// <param name="context">The object context.</param>
        /// <param name="user">The user.</param>
        /// <param name="changeLogEntitySetName">The change log entity set name.</param>
        /// <param name="entry">The changing entry.</param>
        /// <param name="modifiedTime">The change time.</param>
        private static void MakeChangeLogForAddedRelationship<T>(
            ObjectContext context,
            IUser user,
            string changeLogEntitySetName,
            ObjectStateEntry entry, 
            DateTimeOffset modifiedTime)
            where T : IChangeLog, new()
        {
            // relationship must be in the added state.
            CurrentValueRecord currentValues = entry.CurrentValues;
            EntityKey[] values = new EntityKey[2];
            currentValues.GetValues(values);

            foreach (EntityKey key in values)
            {
                ObjectStateEntry changed = context.ObjectStateManager.GetObjectStateEntry(key);
                IBaseEntity entity = changed.Entity as IBaseEntity;
                if (entity != null)
                {
                    EntityKey otherkey = values.Where(p => p != key).Single();
                    IBaseEntity otherentity = context.GetObjectByKey(otherkey) as IBaseEntity;
                    string name = LocatePropertyName(entity, otherentity);
                    if (string.IsNullOrEmpty(name) == false)
                    {
                        string post = GetString(otherkey.EntityKeyValues);
                        IChangeLog log = MakeChangeLog<T>(null, post, Added, user, entity, otherentity, modifiedTime);
                        log.PropertyName = name;
                        context.AddObject(changeLogEntitySetName, log);
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Make a change log for a deleted relationship.
        /// </summary>
        /// <typeparam name="T">The change log type.</typeparam>
        /// <param name="context">The object context.</param>
        /// <param name="user">The user.</param>
        /// <param name="changeLogEntitySetName">The change log entity set name.</param>
        /// <param name="entry">The changing entry.</param>
        /// <param name="modifiedTime">The change time.</param>
        private static void MakeChangeLogForDeletedRelationship<T>(
            ObjectContext context,
            IUser user,
            string changeLogEntitySetName,
            ObjectStateEntry entry, 
            DateTimeOffset modifiedTime)
            where T : IChangeLog, new()
        {
            DbDataRecord originalValues = entry.OriginalValues;
            EntityKey[] values = new EntityKey[2];
            originalValues.GetValues(values);

            foreach (EntityKey key in values)
            {
                ObjectStateEntry changed = context.ObjectStateManager.GetObjectStateEntry(key);
                IBaseEntity entity = context.GetObjectByKey(key) as IBaseEntity;

                // if the current item is a base entity derived class that isn't in the deleted state
                // then it must be a member being removed from a collection. log it as such.
                if (entity != null && changed.State != EntityState.Deleted)
                {
                    EntityKey otherkey = values.Where(p => p != key).Single();
                    IBaseEntity otherentity = context.GetObjectByKey(otherkey) as IBaseEntity;
                    ObjectStateEntry otherstate = context.ObjectStateManager.GetObjectStateEntry(otherkey);

                    string name = LocatePropertyName(entity, otherentity);
                    if (string.IsNullOrEmpty(name) == false)
                    {
                        if (otherstate.State == EntityState.Deleted)
                        {
                            otherentity = null;
                        }

                        string pre = GetString(otherkey.EntityKeyValues);
                        IChangeLog log = MakeChangeLog<T>(pre, null, Removed, user, entity, otherentity, modifiedTime);
                        log.PropertyName = name;
                        context.AddObject(changeLogEntitySetName, log);
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Make a change log for an added entity.
        /// </summary>
        /// <typeparam name="T">The change log type.</typeparam>
        /// <param name="context">The object context.</param>
        /// <param name="user">The user.</param>
        /// <param name="entry">The changing entry.</param>
        /// <param name="added">An added related entity.</param>
        /// <param name="modifiedTime">The change time.</param>
        /// <returns>List of change logs.</returns>
        private static List<IChangeLog> MakeChangeLogForAddedEntry<T>(
            ObjectContext context,
            IUser user,
            ObjectStateEntry entry, 
            IBaseEntity added, 
            DateTimeOffset modifiedTime)
            where T : IChangeLog, new()
        {
            string post = null;
            List<IChangeLog> changes = new List<IChangeLog>();
            IEnumerable<string> modifiedProperties = entry.GetModifiedProperties();

            for (int i = 0; i < entry.CurrentValues.FieldCount; i++)
            {
                string name = entry.CurrentValues.GetName(i);
                if (IsPropertyToBeSkipped(name) == true)
                {
                    continue;
                }

                IDataRecord newdata = entry.CurrentValues[i] as IDataRecord;
                if (newdata != null)
                {
                    for (int j = 0; j < newdata.FieldCount; j++)
                    {
                        string fullName = string.Concat(name, '.', newdata.GetName(j));

                        object propertyValue = newdata.GetValue(j);
                        post = propertyValue.ToString();

                        IChangeLog changeLog = MakeChangeLog<T>(null, post, Inserted, user, added, null, modifiedTime);
                        changeLog.PropertyName = fullName;
                        changes.Add(changeLog);
                    }
                }
                else
                {
                    object propertyValue = entry.CurrentValues[i];
                    post = propertyValue.ToString();

                    IChangeLog changeLog = MakeChangeLog<T>(null, post, Inserted, user, added, null, modifiedTime);
                    changeLog.PropertyName = name;
                    changes.Add(changeLog);
                }
            }

            return changes;
        }

        /// <summary>
        /// Make a change log for a modified entity.
        /// </summary>
        /// <typeparam name="T">The change log type.</typeparam>
        /// <param name="context">The object context.</param>
        /// <param name="user">The user.</param>
        /// <param name="entry">The changing entry.</param>
        /// <param name="changed">A changed related entity.</param>
        /// <param name="modifiedTime">The change time.</param>
        /// <returns>List of change logs.</returns>
        private static List<IChangeLog> MakeChangeLogForModifiedEntry<T>(
            ObjectContext context,
            IUser user,
            ObjectStateEntry entry, 
            IBaseEntity changed, 
            DateTimeOffset modifiedTime)
            where T : IChangeLog, new()
        {
            if (entry.OriginalValues.FieldCount != entry.CurrentValues.FieldCount)
            {
                throw new ArgumentOutOfRangeException("entry");
            }

            string pre = null, post = null;
            List<IChangeLog> changes = new List<IChangeLog>();
            IEnumerable<string> modifiedProperties = entry.GetModifiedProperties();

            foreach (string name in modifiedProperties)
            {
                if (IsPropertyToBeSkipped(name) == true)
                {
                    continue;
                }

                IDataRecord newdata = entry.CurrentValues[name] as IDataRecord;
                IDataRecord olddata = entry.OriginalValues[name] as IDataRecord;
                if (newdata != null && olddata != null)
                {
                    for (int j = 0; j < newdata.FieldCount; j++)
                    {
                        string fullName = string.Concat(name, '.', newdata.GetName(j));

                        object newPropertyValue = newdata.GetValue(j);
                        post = newPropertyValue.ToString();

                        object oldPropertyValue = olddata.GetValue(j);
                        pre = oldPropertyValue.ToString();

                        if (string.CompareOrdinal(pre, post) != 0)
                        {
                            IChangeLog changeLog = MakeChangeLog<T>(pre, post, Updated, user, changed, null, modifiedTime);
                            changeLog.PropertyName = fullName;
                            changes.Add(changeLog);
                        }
                    }
                }
                else
                {
                    object newPropertyValue = entry.CurrentValues[name];
                    post = newPropertyValue.ToString();

                    object oldPropertyValue = entry.OriginalValues[name];
                    pre = oldPropertyValue.ToString();

                    if (string.CompareOrdinal(pre, post) != 0)
                    {
                        IChangeLog changeLog = MakeChangeLog<T>(pre, post, Updated, user, changed, null, modifiedTime);
                        changeLog.PropertyName = name;
                        changes.Add(changeLog);
                    }
                }
            }

            return changes;
        }

        /// <summary>
        /// Make the change log.
        /// </summary>
        /// <param name="pre">The pre value.</param>
        /// <param name="post">The post value.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="user">The user making the change.</param>
        /// <param name="entity">The entity undergoing change.</param>
        /// <param name="related">The related entity.</param>
        /// <param name="modifiedTime">The modified time.</param>
        /// <returns>The created change log.</returns>
        private static IChangeLog MakeChangeLog<T>(
            string pre,
            string post,
            int operation,
            IUser user,
            IBaseEntity entity,
            IBaseEntity related,
            DateTimeOffset modifiedTime)
            where T : IChangeLog, new()
        {
            T changeLog = new T();
            changeLog.Pre = string.IsNullOrEmpty(pre) ? null : pre;
            changeLog.Post = string.IsNullOrEmpty(post) ? null : post;
            changeLog.RelatedEntity = related;
            changeLog.Entity = entity;
            changeLog.ModifiedBy = user;
            changeLog.ChangeTime = modifiedTime;
            changeLog.ChangeLogType = operation;

            return changeLog;
        }

        /// <summary>
        /// Helper to locate the property name.
        /// </summary>
        /// <param name="entity">The entity to inspect.</param>
        /// <param name="other">The other entity.</param>
        /// <returns>The discovered property name.</returns>
        private static string LocatePropertyName(IBaseEntity entity, IBaseEntity other)
        {
            string name = null;
            System.Reflection.PropertyInfo[] properties = entity.GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo pi in properties)
            {
                object self = TypeCache.GetValue(entity.GetType(), pi.Name, entity);
                if (self == other)
                {
                    // value match.
                    name = pi.Name;
                    break;
                }
                else if (other.GetType().BaseType == pi.PropertyType &&
                    string.IsNullOrEmpty(name) == true)
                {
                    // first type match.
                    name = pi.Name;
                }
                else if (other.GetType().BaseType == pi.PropertyType &&
                    string.IsNullOrEmpty(name) == false)
                {
                    // second type match, can not determine property name.
                    name = null;
                    break;
                }
            }

            return name;
        }

        /// <summary>
        /// Helper to serialize entity key members.
        /// </summary>
        /// <param name="members">The members to serialize.</param>
        /// <returns>The resulting string.</returns>
        private static string GetString(EntityKeyMember[] members)
        {
            if (members == null)
            {
                return null;
            }

            return string.Join<EntityKeyMember>(";", members);
        }

        /// <summary>
        /// Helper to determine whether the given entry is a relationship for a change log.
        /// </summary>
        /// <param name="entry">The entry to evaluate.</param>
        /// <param name="changeLogType">The type of the change log.</param>
        /// <returns>True if the entry is a change log relationship, otherwise false.</returns>
        private static bool IsChangeLogRelationship(ObjectStateEntry entry, Type changeLogType)
        {
            return entry.EntitySet.Name.Contains(changeLogType.Name);
        }

        /// <summary>
        /// Helper to determine whether a given BaseEntity property should be skipped for change logging.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>True to skip change log for the property, otherwise false.</returns>
        private static bool IsPropertyToBeSkipped(string name)
        {
            Type baseType = typeof(IBaseEntity);
            IEnumerable<string> properties = baseType.GetProperties().Select(p => p.Name);

            return properties.Contains(name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
