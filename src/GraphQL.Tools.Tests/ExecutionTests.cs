﻿using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GraphQL.Tools.Tests
{
    public class ExecutionTests : TestBase
    {
        class Post
        {
            public string Id { get; set; }
            public string Title { get; set; }
        }

        private readonly List<Post> _posts;

        public ExecutionTests()
        {
            _posts = new List<Post>
            {
                new Post {Id = "1", Title = "Post One"}
            };
        }

        abstract class Pet
        {
            public string Name { get; set; }
        }

        class Dog : Pet
        {
            public bool Barks { get; set; }
        }

        class Cat : Pet
        {
            public bool Meows { get; set; }
        }

        enum PetKind
        {
            Cat,
            Dog
        }

        [Fact]
        public void can_execute_resolver()
        {
            var defs = @"
                type Post {
                    id: ID!
                    title: String!
                }

                type Query {
                    post(id: ID = 1): Post
                }
            ";

            Builder.Config("Query", _ =>
            {
                _.Resolver("post", context =>
                {
                    var id = context.GetArgument<string>("id");
                    return _posts.FirstOrDefault(x => x.Id == id);
                });
            });

            var query = @"{ post { id title } }";
            var expected = @"{ 'post': { 'id' : '1', 'title': 'Post One' } }";

            AssertQuery(_ =>
            {
                _.Query = query;
                _.Definitions = defs;
                _.ExpectedResult = expected;
            });
        }

        [Fact]
        public void can_execute_interfaces()
        {
            var defs = @"
                enum PetKind {
                    CAT
                    DOG
                }

                interface Pet {
                    name: String!
                }

                type Dog implements Pet {
                    name: String!
                    barks: Boolean!
                }

                type Cat implements Pet {
                    name: String!
                    meows: Boolean!
                }

                type Query {
                    pet(type: PetKind = DOG): Pet
                }
            ";

            Builder.Config("Dog", _ =>
            {
                _.IsTypeOf = obj => obj is Dog;
            });

            Builder.Config("Cat", _ =>
            {
                _.IsTypeOf = obj => obj is Cat;
            });

            Builder.Config("Query", _ =>
            {
                _.Resolver<Pet>("pet", context =>
                {
                    var type = context.GetArgument<PetKind>("type");
                    if (type == PetKind.Dog)
                    {
                        return new Dog {Name = "Eli", Barks = true};
                    }

                    return new Cat {Name = "Biscuit", Meows = true};
                });
            });

            var query = @"{ pet { name } }";
            var expected = @"{ 'pet': { 'name' : 'Eli' } }";

            AssertQuery(_ =>
            {
                _.Query = query;
                _.Definitions = defs;
                _.ExpectedResult = expected;
            });
        }
    }
}