﻿using System;
using System.Collections.Generic;

namespace Entitas {

    public delegate void EntityChanged(Entity entity, int index, IComponent component);
    public delegate void ComponentReplaced(Entity entity, int index, IComponent previousComponent, IComponent newComponent);
    public delegate void EntityReleased(Entity entity);

    public interface IEntity {

        event EntityChanged OnComponentAdded;
        event EntityChanged OnComponentRemoved;
        event ComponentReplaced OnComponentReplaced;
        event EntityReleased OnEntityReleased;

        int totalComponents { get; }
        int creationIndex { get; }
        bool isEnabled { get; }

        Stack<IComponent>[] componentPools { get; }
        ContextInfo contextInfo { get; }

        void AddComponent(int index, IComponent component);
        void RemoveComponent(int index);
        void ReplaceComponent(int index, IComponent component);

        IComponent GetComponent(int index);
        IComponent[] GetComponents();
        int[] GetComponentIndices();

        bool HasComponent(int index);
        bool HasComponents(int[] indices);
        bool HasAnyComponent(int[] indices);

        void RemoveAllComponents();

        Stack<IComponent> GetComponentPool(int index);
        IComponent CreateComponent(int index, Type type);
        T CreateComponent<T>(int index) where T : new();

        int retainCount { get; }
        void Retain(object owner);
        void Release(object owner);
    }
}