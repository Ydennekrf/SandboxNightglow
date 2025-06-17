using Godot;
using System;

    public interface IStateAction
    {

        void Enter(Entity owner, BaseState baseState);


        void Execute(float delta, Entity owner, BaseState baseState);


        void Exit(Entity owner);
    }