# 3CXPhonePlugin

The 3CXPhone for Windows Plugin API is designed to allow customers and developers to easily integrate the applications they use with the 3CXPhone client for Windows. The 3CXPhone for Windows API gives you the opportunity to launch and control calls and collect call notifications.

This repository includes sample projects which can be used as a seed for developing 3CXPhone plugin.

- CallTriggerCmdPlugin is a sample plugin which is included in 3CXPhone installation by default. This is a plugin seed. CallTriggerCmdPlugin provides a WCF (Windows Communication Foundation) service through a named pipe which exposes the same functionality as a standard plugin. So instead of writing a plugin you can write a WCF client.

- CallTriggerCmdServiceProvider is a WCF description for CallTriggerCmdPlugin

- CallTriggerCmd is a sample client of CallTriggerCmdPlugin
