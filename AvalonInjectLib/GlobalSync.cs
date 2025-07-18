﻿using System.Diagnostics;

namespace AvalonInjectLib
{
    public static class GlobalSync
    {
        // Lock principal para operaciones críticas
        public static readonly object MainLock = new object();

        // Semáforo para controlar llamadas remotas durante el renderizado
        private static readonly SemaphoreSlim _remoteCallSemaphore = new SemaphoreSlim(1, 1);

        // Flag para indicar si estamos en medio de un frame de renderizado
        private static volatile bool _isRendering = false;

        // Tiempo de espera para sincronización (en ms)
        public static int SyncTimeout { get; set; } = 50;

        public static bool IsRendering => _isRendering;

        /// <summary>
        /// Intenta iniciar un frame de renderizado de forma no bloqueante
        /// </summary>
        /// <param name="renderScope">Scope para el renderizado que debe ser disposed al finalizar</param>
        /// <returns>true si se pudo adquirir el lock, false si no</returns>
        public static bool TryBeginRender(out IDisposable renderScope)
        {
            renderScope = null;
            var lockTaken = false;

            try
            {
                Monitor.TryEnter(MainLock, SyncTimeout, ref lockTaken);
                if (!lockTaken)
                {
                    return false;
                }

                _isRendering = true;
                renderScope = new RenderLock();
                return true;
            }
            catch
            {
                if (lockTaken)
                {
                    Monitor.Exit(MainLock);
                }
                renderScope?.Dispose();
                renderScope = null;
                return false;
            }
        }

        /// <summary>
        /// Versión bloqueante del BeginRender para compatibilidad
        /// </summary>
        public static IDisposable BeginRender()
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(MainLock, SyncTimeout, ref lockTaken);
                if (!lockTaken)
                {
                    Logger.Warning("Timeout al adquirir lock de renderizado", "GlobalSync");
                    throw new TimeoutException("No se pudo adquirir el lock de renderizado");
                }

                _isRendering = true;
                return new RenderLock();
            }
            catch
            {
                if (lockTaken) Monitor.Exit(MainLock);
                throw;
            }
        }

        /// <summary>
        /// Inicia una operación remota de forma asíncrona
        /// </summary>
        public static async Task<IDisposable> BeginRemoteOperationAsync()
        {
            if (!await _remoteCallSemaphore.WaitAsync(SyncTimeout))
            {
                Logger.Warning("Timeout al esperar semáforo para operación remota", "GlobalSync");
                throw new TimeoutException("No se pudo iniciar operación remota");
            }

            try
            {
                // Esperar si estamos en medio de un frame de renderizado
                var sw = Stopwatch.StartNew();
                while (_isRendering && sw.ElapsedMilliseconds < SyncTimeout)
                {
                    await Task.Delay(5);
                }

                if (_isRendering)
                {
                    Logger.Warning("Render en progreso, operación remota pospuesta", "GlobalSync");
                    throw new TimeoutException("Render en progreso");
                }

                return new RemoteOperationLock();
            }
            catch
            {
                _remoteCallSemaphore.Release();
                throw;
            }
        }

        /// <summary>
        /// Versión sincrónica para operaciones remotas rápidas
        /// </summary>
        public static bool TryBeginRemoteOperation(out IDisposable remoteScope)
        {
            remoteScope = null;

            if (!_remoteCallSemaphore.Wait(SyncTimeout))
            {
                return false;
            }

            try
            {
                // Verificar si estamos renderizando
                if (_isRendering)
                {
                    return false;
                }

                remoteScope = new RemoteOperationLock();
                return true;
            }
            catch
            {
                _remoteCallSemaphore.Release();
                remoteScope?.Dispose();
                remoteScope = null;
                return false;
            }
        }

        private class RenderLock : IDisposable
        {
            private bool _disposed = false;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    try
                    {
                        lock (MainLock)
                        {
                            _isRendering = false;
                            Monitor.PulseAll(MainLock);
                        }
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
            }
        }

        private class RemoteOperationLock : IDisposable
        {
            private bool _disposed = false;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    try
                    {
                        _remoteCallSemaphore.Release();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
            }
        }
    }
}