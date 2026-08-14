# Erro de Build Identificado
- **Mensagem**: MSBUILD : error MSB4166: Child node "2" exited prematurely
- **Causa Probável**: Problema com instalação do Android SDK ou dependências em /usr/local/android-sdk
- **Ação Adotada**:
1. Modificado AndroidSDKDirectory para /usr/local/android-sdk (confere correta instalação)
2. Atualizado Android API level para 36 na configuração do Codemagic