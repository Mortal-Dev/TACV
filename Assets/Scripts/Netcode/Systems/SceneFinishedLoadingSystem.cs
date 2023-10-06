using Unity.Entities;
using Unity.Scenes;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SceneFinishedLoadingSystem : SystemBase
{
    private bool scenesFinishedLoading = false;

    private bool calledEvent = false;

    public delegate void FinishedLoadingSubScenes();

    public event FinishedLoadingSubScenes FinishedLoadingSubScenesCompleted;

    private bool startChecking = false;

    public void StartChecking()
    {
        startChecking = true;
    }

    public void StopChecking()
    {
        startChecking = false;

        scenesFinishedLoading = false;
        calledEvent = false;
    }

    protected override void OnUpdate()
    {
        if (!startChecking) return;

        if (scenesFinishedLoading)
        {
            if (!calledEvent)
            {
                FinishedLoadingSubScenesCompleted?.Invoke();
                calledEvent = true;
            }
        }

        int count = 1;

        int counter = 0;

        Entities.ForEach((Entity entity, in SceneReference sceneReference) =>
        {
            if (SceneSystem.IsSceneLoaded(EntityManager.WorldUnmanaged, entity))
            {
                counter++;
            }

        }).WithoutBurst().Run();

        if (counter != count)
        {
            scenesFinishedLoading = false;
            calledEvent = false;
        }
        else
        {
            scenesFinishedLoading = true;
        }
    }
}
