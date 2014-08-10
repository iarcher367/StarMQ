﻿namespace StarMQ.Test.Message
{
    using NUnit.Framework;
    using StarMQ.Message;
    using System;

    public class JsonSerializerTest
    {
        private const string Content = "Hello world!";
        private ISerializer _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new JsonSerializer();
        }

        [Test]
        public void ShouldUseTypeNameHandlingAuto()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void ShouldDeserializeByteArrayToObject()
        {
            var input = new Byte[] { 34, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 33, 34 };

            var actual = _sut.ToObject<string>(input);

            Assert.That(actual, Is.EqualTo(Content));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldToObjectThrowExceptionIfContentIsNull()
        {
            _sut.ToObject<string>(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldToObjectThrowExceptionIfContentIsEmpty()
        {
            _sut.ToObject<string>(new byte[0]);
        }

        [Test]
        public void ShouldSerializeObjectToByteArray()
        {
            var expected = new Byte[] { 34, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 33, 34 };

            var actual = _sut.ToBytes(Content);

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldToBytesThrowExceptionIfContentIsNull()
        {
            _sut.ToBytes<string>(null);
        }
    }
}