using System;
using LiveKit.Internal.FFIClients.Pools;
using LiveKit.Proto;

namespace LiveKit.Internal.FFIClients.Requests
{
    public struct FfiRequestWrap<T> : IDisposable where T : class, new()
    {
        public readonly T request;
        private readonly IFFIClient ffiClient;
        private readonly FfiRequest ffiRequest;
        private readonly Action<FfiRequest> releaseFfiRequest;
        private readonly Action<T> releaseRequest;

        private bool sent;

        public FfiRequestWrap(IFFIClient ffiClient, IMultiPool multiPool) : this(
            multiPool.Get<T>(),
            multiPool.Get<FfiRequest>(),
            ffiClient,
            multiPool.Release,
            multiPool.Release
        )
        {
        }

        public FfiRequestWrap(
            T request,
            FfiRequest ffiRequest,
            IFFIClient ffiClient,
            Action<FfiRequest> releaseFfiRequest,
            Action<T> releaseRequest
        )
        {
            this.request = request;
            this.ffiRequest = ffiRequest;
            this.ffiClient = ffiClient;
            this.releaseFfiRequest = releaseFfiRequest;
            this.releaseRequest = releaseRequest;
            sent = false;
        }

        public FfiResponseWrap Send()
        {
            if (sent)
            {
                throw new Exception("Request already sent");
            }

            sent = true;
            ffiRequest.Inject(request);
            var response = ffiClient.SendRequest(ffiRequest);
            return new FfiResponseWrap(response, ffiClient);
        }

        public void Dispose()
        {
            ffiRequest.ClearMessage();
            releaseRequest(request);
            releaseFfiRequest(ffiRequest);
        }
    }
}