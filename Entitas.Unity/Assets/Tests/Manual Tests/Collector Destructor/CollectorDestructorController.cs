using UnityEngine;
using Entitas;
using UnityEditor;

public class CollectorDestructorController : MonoBehaviour {

    Entity _initialEntity;

    void Start() {
        var context = Contexts.sharedInstance.visualDebugging = Contexts.CreateVisualDebuggingContext();
        context.GetGroup(VisualDebuggingMatcher.Test).CreateCollector();
        _initialEntity = context.CreateEntity();
        _initialEntity.isTest = true;
        context.DestroyEntity(_initialEntity);
        context.ClearGroups();
    }
	
    void Update() {
        var context = Contexts.sharedInstance.visualDebugging;
        for (int i = 0; i < 5000; i++) {
            var e = context.CreateEntity();
            if(e == _initialEntity) {
                Debug.Log("Reusing entity!");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
