using System;
using R3;
using ShapeEngine.Core.Structs;

namespace ShapeEngine.Timing.R3;

public static class MonoGameObservableExtensions
{
    /// <summary>
    /// Observe the current GameTime once.
    /// </summary>
    public static Observable<GameTime> GameTime(this Observable<Unit> source)
    {
        return new GameTimeObservable(source, ReactiveTimeProvider.Update);
    }

    /// <summary>
    /// Observes the current GameTime and the value of the source observable.
    /// </summary>
    public static Observable<(GameTime GameTime, T Item)> GameTime<T>(this Observable<T> source)
    {
        return new GameTimeObservable<T>(source,  ReactiveTimeProvider.Update);
    }
}

internal sealed class GameTimeObservable(Observable<Unit> source, ReactiveTimeProvider frameProvider) : Observable<GameTime>
{
    protected override IDisposable SubscribeCore(Observer<GameTime> observer)
    {
        return source.Subscribe(new _GameTime(observer, frameProvider));
    }

    sealed class _GameTime(Observer<GameTime> observer, ReactiveTimeProvider timeProvider) : Observer<Unit>
    {
        protected override void OnNextCore(Unit value)
        {
            observer.OnNext(timeProvider.GameTime);
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnErrorResume(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            observer.OnCompleted(result);
        }
    }
}

internal sealed class GameTimeObservable<T>(Observable<T> source, ReactiveTimeProvider timeProvider) : Observable<(GameTime gameTime, T Item)>
{
    protected override IDisposable SubscribeCore(Observer<(GameTime gameTime, T Item)> observer)
    {
        return source.Subscribe(new _GameTime(observer, timeProvider));
    }

    sealed class _GameTime(Observer<(GameTime GameTime, T Item)> observer, ReactiveTimeProvider timeProvider) : Observer<T>
    {
        protected override void OnNextCore(T value)
        {
            observer.OnNext((timeProvider.GameTime, value));
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnErrorResume(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            observer.OnCompleted(result);
        }
    }
}
