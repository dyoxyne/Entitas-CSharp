//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGenerator.ContextsGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Entitas {

    public partial class Contexts {

        public static Context CreateContext() {
            return CreateContext("Context", ComponentIds.TotalComponents, ComponentIds.componentNames, ComponentIds.componentTypes);
        }

        public Context[] allContexts { get { return new [] { context }; } }

        public Context context;

        public void SetAllContexts() {
            context = CreateContext();
        }
    }
}
