using System;
using System.Collections.Generic;
using System.Text;

namespace Entitas {

    /// Use context.CreateEntity() to create a new entity and
    /// context.DestroyEntity() to destroy it.
    /// You can add, replace and remove IComponent to an entity.
    public partial class Entity {

        /// Occurs when a component gets added.
        /// All event handlers will be removed when
        /// the entity gets destroyed by the context.
        public event EntityChanged OnComponentAdded;

        /// Occurs when a component gets removed.
        /// All event handlers will be removed when
        /// the entity gets destroyed by the context.
        public event EntityChanged OnComponentRemoved;

        /// Occurs when a component gets replaced.
        /// All event handlers will be removed when
        /// the entity gets destroyed by the context.
        public event ComponentReplaced OnComponentReplaced;

        /// Occurs when an entity gets released and is not retained anymore.
        /// All event handlers will be removed when
        /// the entity gets destroyed by the context.
        public event EntityReleased OnEntityReleased;

        public delegate void EntityChanged(
            Entity entity, int index, IComponent component
        );

        public delegate void ComponentReplaced(
            Entity entity, int index,
            IComponent previousComponent, IComponent newComponent
        );

        public delegate void EntityReleased(Entity entity);

        /// The total amount of components an entity can possibly have.
        public int totalComponents { get { return _totalComponents; } }

        /// Each entity has its own unique creationIndex which will be set by
        /// the context when you create the entity.
        public int creationIndex { get { return _creationIndex; } }

        /// The context manages the state of an entity.
        /// Active entities are enabled, destroyed entities are not.
        public bool isEnabled { get { return _isEnabled; } }

        /// componentPools is set by the context which created the entity and
        /// is used to reuse removed components.
        /// Removed components will be pushed to the componentPool.
        /// Use entity.CreateComponent(index, type) to get a new or
        /// reusable component from the componentPool.
        /// Use entity.GetComponentPool(index) to get a componentPool for
        /// a specific component index.
        public Stack<IComponent>[] componentPools {
            get { return _componentPools; }
        }

        /// The contextInfo is set by the context which created the entity and
        /// contains information about the context.
        /// It's used to provide better error messages.
        public ContextInfo contextInfo { get { return _contextInfo; } }

        internal int _creationIndex;
        internal bool _isEnabled = true;

        readonly int _totalComponents;
        readonly IComponent[] _components;
        readonly Stack<IComponent>[] _componentPools;
        readonly ContextInfo _contextInfo;

        IComponent[] _componentsCache;
        int[] _componentIndicesCache;
        string _toStringCache;
        StringBuilder _toStringBuilder;

        /// Use context.CreateEntity() to create a new entity and
        /// context.DestroyEntity() to destroy it.
        public Entity(int totalComponents,
                      Stack<IComponent>[] componentPools,
                      ContextInfo contextInfo = null) {
            _totalComponents = totalComponents;
            _components = new IComponent[totalComponents];
            _componentPools = componentPools;

            if(contextInfo != null) {
                _contextInfo = contextInfo;
            } else {

                // If context.CreateEntity() was used to create the entity,
                // we will never end up here.
                // This is a fallback when entities are created manually.

                var componentNames = new string[totalComponents];
                for(int i = 0; i < componentNames.Length; i++) {
                    componentNames[i] = i.ToString();
                }
                _contextInfo = new ContextInfo(
                    "No Context", componentNames, null
                );
            }
        }

        /// Adds a component at the specified index.
        /// You can only have one component at an index.
        /// Each component type must have its own constant index.
        /// The prefered way is to use the
        /// generated methods from the code generator.
        public void AddComponent(int index, IComponent component) {
            if(!_isEnabled) {
                throw new EntityIsNotEnabledException(
                    "Cannot add component '" +
                    _contextInfo.componentNames[index] +
                    "' to " + this + "!"
                );
            }

            if(HasComponent(index)) {
                throw new EntityAlreadyHasComponentException(
                    index,
                    "Cannot add component '" +
                    _contextInfo.componentNames[index] +
                    "' to " + this + "!",
                    "You should check if an entity already has the component " +
                    "before adding it or use entity.ReplaceComponent()."
                );
            }

            _components[index] = component;
            _componentsCache = null;
            _componentIndicesCache = null;
            _toStringCache = null;
            if(OnComponentAdded != null) {
                OnComponentAdded(this, index, component);
            }
        }

        /// Removes a component at the specified index.
        /// You can only remove a component at an index if it exists.
        /// The prefered way is to use the
        /// generated methods from the code generator.
        public void RemoveComponent(int index) {
            if(!_isEnabled) {
                throw new EntityIsNotEnabledException(
                    "Cannot remove component '" +
                    _contextInfo.componentNames[index] +
                    "' from " + this + "!"
                );
            }

            if(!HasComponent(index)) {
                throw new EntityDoesNotHaveComponentException(
                    index,
                    "Cannot remove component '" +
                    _contextInfo.componentNames[index] +
                    "' from " + this + "!",
                    "You should check if an entity has the component " +
                    "before removing it."
                );
            }

            replaceComponent(index, null);
        }

        /// Replaces an existing component at the specified index
        /// or adds it if it doesn't exist yet.
        /// The prefered way is to use the
        /// generated methods from the code generator.
        public void ReplaceComponent(int index, IComponent component) {
            if(!_isEnabled) {
                throw new EntityIsNotEnabledException(
                    "Cannot replace component '" +
                    _contextInfo.componentNames[index] +
                    "' on " + this + "!"
                );
            }

            if(HasComponent(index)) {
                replaceComponent(index, component);
            } else if(component != null) {
                AddComponent(index, component);
            }
        }

        void replaceComponent(int index, IComponent replacement) {
            _toStringCache = null;
            var previousComponent = _components[index];
            if(replacement != previousComponent) {
                _components[index] = replacement;
                _componentsCache = null;
                if(replacement != null) {
                    if(OnComponentReplaced != null) {
                        OnComponentReplaced(
                            this, index, previousComponent, replacement
                        );
                    }
                } else {
                    _componentIndicesCache = null;
                    if(OnComponentRemoved != null) {
                        OnComponentRemoved(this, index, previousComponent);
                    }
                }

                GetComponentPool(index).Push(previousComponent);

            } else {
                if(OnComponentReplaced != null) {
                    OnComponentReplaced(
                        this, index, previousComponent, replacement
                    );
                }
            }
        }

        /// Returns a component at the specified index.
        /// You can only get a component at an index if it exists.
        /// The prefered way is to use the
        /// generated methods from the code generator.
        public IComponent GetComponent(int index) {
            if(!HasComponent(index)) {
                throw new EntityDoesNotHaveComponentException(
                    index,
                    "Cannot get component '" +
                    _contextInfo.componentNames[index] + "' from " +
                    this + "!",
                    "You should check if an entity has the component " +
                    "before getting it."
                );
            }

            return _components[index];
        }

        /// Returns all added components.
        public IComponent[] GetComponents() {
            if(_componentsCache == null) {
                var components = EntitasCache.GetIComponentList();

                    for(int i = 0; i < _components.Length; i++) {
                        var component = _components[i];
                        if(component != null) {
                            components.Add(component);
                        }
                    }

                    _componentsCache = components.ToArray();

                EntitasCache.PushIComponentList(components);
            }

            return _componentsCache;
        }

        /// Returns all indices of added components.
        public int[] GetComponentIndices() {
            if(_componentIndicesCache == null) {
                var indices = EntitasCache.GetIntList();

                    for(int i = 0; i < _components.Length; i++) {
                        if(_components[i] != null) {
                            indices.Add(i);
                        }
                    }

                    _componentIndicesCache = indices.ToArray();

                EntitasCache.PushIntList(indices);
            }

            return _componentIndicesCache;
        }

        /// Determines whether this entity has a component
        /// at the specified index.
        public bool HasComponent(int index) {
            return _components[index] != null;
        }

        /// Determines whether this entity has components
        /// at all the specified indices.
        public bool HasComponents(int[] indices) {
            for(int i = 0; i < indices.Length; i++) {
                if(_components[indices[i]] == null) {
                    return false;
                }
            }

            return true;
        }

        /// Determines whether this entity has a component
        /// at any of the specified indices.
        public bool HasAnyComponent(int[] indices) {
            for(int i = 0; i < indices.Length; i++) {
                if(_components[indices[i]] != null) {
                    return true;
                }
            }

            return false;
        }

        /// Removes all components.
        public void RemoveAllComponents() {
            _toStringCache = null;
            for(int i = 0; i < _components.Length; i++) {
                if(_components[i] != null) {
                    replaceComponent(i, null);
                }
            }
        }

        /// Returns the componentPool for the specified component index.
        /// componentPools is set by the context which created the entity and
        /// is used to reuse removed components.
        /// Removed components will be pushed to the componentPool.
        /// Use entity.CreateComponent(index, type) to get a new or
        /// reusable component from the componentPool.
        public Stack<IComponent> GetComponentPool(int index) {
            var componentPool = _componentPools[index];
            if(componentPool == null) {
                componentPool = new Stack<IComponent>();
                _componentPools[index] = componentPool;
            }

            return componentPool;
        }

        /// Returns a new or reusable component from the componentPool
        /// for the specified component index.
        public IComponent CreateComponent(int index, Type type) {
            var componentPool = GetComponentPool(index);
            return componentPool.Count > 0
                        ? componentPool.Pop()
                        : (IComponent)Activator.CreateInstance(type);
        }

        /// Returns a new or reusable component from the componentPool
        /// for the specified component index.
        public T CreateComponent<T>(int index) where T : new() {
            var componentPool = GetComponentPool(index);
            return componentPool.Count > 0 ? (T)componentPool.Pop() : new T();
        }

#if ENTITAS_FAST_AND_UNSAFE
        
        /// Returns the number of objects that retain this entity.
        public int retainCount { get { return _retainCount; } }
        int _retainCount;

#else

        /// Returns the number of objects that retain this entity.
        public int retainCount { get { return owners.Count; } }

        /// Returns all the objects that retain this entity.
        public readonly HashSet<object> owners = new HashSet<object>();

#endif

        /// Retains the entity. An owner can only retain the same entity once.
        /// Retain/Release is part of AERC (Automatic Entity Reference Counting)
        /// and is used internally to prevent pooling retained entities.
        /// If you use retain manually you also have to
        /// release it manually at some point.
        public void Retain(object owner) {

#if ENTITAS_FAST_AND_UNSAFE
            
            _retainCount += 1;

#else

            if(!owners.Add(owner)) {
                throw new EntityIsAlreadyRetainedByOwnerException(this, owner);
            }

#endif

            _toStringCache = null;
        }

        /// Releases the entity. An owner can only release an entity
        /// if it retains it.
        /// Retain/Release is part of AERC (Automatic Entity Reference Counting)
        /// and is used internally to prevent pooling retained entities.
        /// If you use retain manually you also have to
        /// release it manually at some point.
        public void Release(object owner) {

#if ENTITAS_FAST_AND_UNSAFE
            
            _retainCount -= 1;
            if(_retainCount == 0) {

#else

            if(!owners.Remove(owner)) {
                throw new EntityIsNotRetainedByOwnerException(this, owner);
            }

            if(owners.Count == 0) {

#endif

                _toStringCache = null;

                if(OnEntityReleased != null) {
                    OnEntityReleased(this);
                }
            }
        }

        // This method is used internally. Don't call it yourself.
        // Use context.DestroyEntity(entity);
        internal void destroy() {
            _isEnabled = false;
            RemoveAllComponents();
            OnComponentAdded = null;
            OnComponentReplaced = null;
            OnComponentRemoved = null;
        }

        // Do not call this method manually. This method is called by the context.
        internal void removeAllOnEntityReleasedHandlers() {
            OnEntityReleased = null;
        }

        /// Returns a cached string to describe the entity
        /// with the following format:
        /// Entity_{creationIndex}(*{retainCount})({list of components})
        public override string ToString() {
            if(_toStringCache == null) {
                if(_toStringBuilder == null) {
                    _toStringBuilder = new StringBuilder();
                }
                _toStringBuilder.Length = 0;
                _toStringBuilder
                    .Append("Entity_")
                    .Append(_creationIndex)
                    .Append("(*")
                    .Append(retainCount)
                    .Append(")")
                    .Append("(");

                const string separator = ", ";
                var components = GetComponents();
                var lastSeparator = components.Length - 1;
                for(int i = 0; i < components.Length; i++) {
                    var component = components[i];
                    var type = component.GetType();
                    var implementsToString = type.GetMethod("ToString")
                                                 .DeclaringType == type;
                    _toStringBuilder.Append(
                        implementsToString
                            ? component.ToString()
                            : type.Name.RemoveComponentSuffix()
                    );

                    if(i < lastSeparator) {
                        _toStringBuilder.Append(separator);
                    }
                }

                _toStringBuilder.Append(")");
                _toStringCache = _toStringBuilder.ToString();
            }

            return _toStringCache;
        }
    }

    public class EntityAlreadyHasComponentException : EntitasException {

        public EntityAlreadyHasComponentException(
            int index, string message, string hint
        ) : base(
                message +
                "\nEntity already has a component at index "
                + index + "!",
                hint
            ) {
        }
    }

    public class EntityDoesNotHaveComponentException : EntitasException {

        public EntityDoesNotHaveComponentException(
            int index, string message, string hint
        ) : base(
                message +
                "\nEntity does not have a component at index "
                + index + "!",
                hint
            ) {
        }
    }

    public class EntityIsNotEnabledException : EntitasException {

        public EntityIsNotEnabledException(string message) :
            base(
                message + "\nEntity is not enabled!",
                "The entity has already been destroyed. " +
                "You cannot modify destroyed entities."
            ) {
        }
    }

    public class EntityEqualityComparer : IEqualityComparer<Entity> {

        public static readonly EntityEqualityComparer comparer =
            new EntityEqualityComparer();

        public bool Equals(Entity x, Entity y) {
            return x == y;
        }

        public int GetHashCode(Entity obj) {
            return obj._creationIndex;
        }
    }

    public class EntityIsAlreadyRetainedByOwnerException : EntitasException {

        public EntityIsAlreadyRetainedByOwnerException(
            Entity entity, object owner
        ) : base(
                "'" + owner + "' cannot retain " + entity + "!\n" +
                "Entity is already retained by this object!",
                "The entity must be released by this object first."
            ) {
        }
    }

    public class EntityIsNotRetainedByOwnerException : EntitasException {

        public EntityIsNotRetainedByOwnerException(Entity entity, object owner) :
            base(
                "'" + owner + "' cannot release " + entity + "!\n" +
                "Entity is not retained by this object!",
                "An entity can only be released from objects that retain it."
            ) {
        }
    }
}
