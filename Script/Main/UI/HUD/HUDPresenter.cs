using UnityEngine;

public class HUDPresenter
{
    HUDModel model;
    HUDView view;   

    public HUDPresenter(HUDModel model, HUDView view)
    {
        this.model = model;
        this.view = view;

        model.Reset();
        EventBus.Subscribe<AddScoreEvent>(OnUpdateScore);
        EventBus.Subscribe<InitScoreEvent>(OnInitScore);
        EventBus.Subscribe<OnChangeCoin>(OnAddCoin);
    }

    private void OnInitScore(InitScoreEvent e)
    {
        model.InitHighScore(e.highScore);
        view.SetHighScore(e.highScore);
    }

    public void OnUpdateScore(AddScoreEvent e)
    {
        model.UpdateScore(e.score);
        view.SetScore(model.Score);
        view.SetHighScore(model.HighScore);
    }

    private void OnAddCoin(OnChangeCoin e)
    {
        model.AddCoin(e.coin);
        view.SetCoin(model.Coin);
    }


    public void UpdateTimer(float time)
    {
        view.SetTimer(time);
    }


    public void Dispose()
    {
        EventBus.Unsubscribe<AddScoreEvent>(OnUpdateScore);
        EventBus.Unsubscribe<InitScoreEvent>(OnInitScore);
        EventBus.Unsubscribe<OnChangeCoin>(OnAddCoin);
    }
}